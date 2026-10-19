using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Retired.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Retired.Queries;

public class GetRetiredImpactQuery : IRequest<RetiredImpactResponse>
{
    public RetiredType Type { get; set; }
    public Guid Id { get; set; }
}

public class GetRetiredImpactQueryHandler : IRequestHandler<GetRetiredImpactQuery, RetiredImpactResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;

    public GetRetiredImpactQueryHandler(IApplicationDbContext dbContext, MinistryAccess access)
    {
        _dbContext = dbContext;
        _access = access;
    }

    public async Task<RetiredImpactResponse> Handle(GetRetiredImpactQuery request, CancellationToken cancellationToken = default)
    {
        RetiredRecordHelpers.EnsureOwner(_access, request.Type);
        var id = request.Id;

        switch (request.Type)
        {
            case RetiredType.Attendee:
                var attendee = await RetiredRecordHelpers.RetiredAttendeeAsync(_dbContext, id, cancellationToken);
                return Impact(request, attendee.FullName, null, new()
                {
                    ["groupHistory"] = await _dbContext.GroupMemberships.CountAsync(item => item.AttendeeId == id, cancellationToken),
                    ["attendanceMarks"] = await _dbContext.AttendeeAttendances.IgnoreQueryFilters().CountAsync(item => item.AttendeeId == id, cancellationToken),
                    ["followUps"] = await _dbContext.FollowUps.IgnoreQueryFilters().CountAsync(item => item.SubjectAttendeeId == id, cancellationToken),
                    ["contacts"] = await _dbContext.AttendeeContacts.CountAsync(item => item.AttendeeId == id, cancellationToken),
                    ["courses"] = await _dbContext.AttendeeCourses.CountAsync(item => item.AttendeeId == id, cancellationToken)
                });

            case RetiredType.Group:
                var group = await RetiredRecordHelpers.RetiredGroupAsync(_dbContext, id, cancellationToken);
                var memberships = await _dbContext.GroupMemberships.IgnoreQueryFilters().CountAsync(item => item.GroupId == id, cancellationToken);
                var events = await _dbContext.EventGroups.CountAsync(item => item.GroupId == id, cancellationToken);
                return Impact(request, group.Name,
                    memberships + events > 0 ? "The group is part of attendees' history or events and cannot be permanently deleted." : null,
                    new() { ["groupHistory"] = memberships, ["events"] = events, ["team"] = await _dbContext.GroupAssignments.CountAsync(item => item.GroupId == id, cancellationToken) });

            case RetiredType.Event:
                var @event = await RetiredRecordHelpers.RetiredEventAsync(_dbContext, id, cancellationToken);
                return Impact(request, @event.Title, null, new()
                {
                    ["attendanceMarks"] = await _dbContext.AttendeeAttendances.IgnoreQueryFilters().CountAsync(item => item.EventId == id, cancellationToken)
                        + await _dbContext.UserAttendances.IgnoreQueryFilters().CountAsync(item => item.EventId == id, cancellationToken),
                    ["comments"] = await _dbContext.EventComments.IgnoreQueryFilters().CountAsync(item => item.EventId == id, cancellationToken),
                    ["files"] = await _dbContext.EventAttachments.IgnoreQueryFilters().CountAsync(item => item.EventId == id, cancellationToken)
                        + await _dbContext.EventCommentAttachments.IgnoreQueryFilters().CountAsync(item => item.Comment.EventId == id, cancellationToken)
                });

            case RetiredType.FollowUp:
                await RetiredRecordHelpers.RetiredFollowUpAsync(_dbContext, _access, id, cancellationToken);
                return Impact(request, "Seguimiento", null, new());

            default:
                var (label, references) = await CatalogReferencesAsync(request.Type, id, cancellationToken);
                return Impact(request, label, references > 0 ? "The value is used by existing records and cannot be permanently deleted." : null,
                    new() { ["references"] = references });
        }
    }

    private static RetiredImpactResponse Impact(GetRetiredImpactQuery request, string label, string? blockedReason, Dictionary<string, int> dependencies) =>
        new RetiredImpactResponse { Type = request.Type, Id = request.Id, Label = label, Dependencies = dependencies, CanDelete = blockedReason is null, BlockedReason = blockedReason };

    private async Task<(string Label, int References)> CatalogReferencesAsync(RetiredType type, Guid id, CancellationToken cancellationToken)
    {
        var item = await RetiredRecordHelpers.FindCatalogAsync(_dbContext, type, id, cancellationToken);
        var references = type switch
        {
            RetiredType.Generation => await _dbContext.Groups.IgnoreQueryFilters().CountAsync(group => group.GenerationId == id, cancellationToken)
                + await _dbContext.EventGenerations.CountAsync(link => link.GenerationId == id, cancellationToken),
            RetiredType.EventType => await _dbContext.Events.IgnoreQueryFilters().CountAsync(@event => @event.EventTypeId == id, cancellationToken),
            RetiredType.FollowUpTopic => await _dbContext.FollowUps.IgnoreQueryFilters().CountAsync(followUp => followUp.TopicId == id, cancellationToken),
            _ => await _dbContext.AttendeeCourses.CountAsync(course => course.CourseId == id, cancellationToken)
        };

        return (item.Name, references);
    }
}
