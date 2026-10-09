using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps;

/// <summary>Seguimientos visibles (S07): pendientes o realizados, con datos generales.</summary>
public class GetFollowUpsQuery : IRequest<PagedResult<FollowUpListItem>>
{
    public FollowUpStatus Status { get; set; } = FollowUpStatus.Pending;
    public Guid? GroupId { get; set; }
    public string? Search { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }

    /// <summary>Solo pendientes sin fecha programada.</summary>
    public bool? WithoutDate { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

/// <summary>
/// Detalle de un seguimiento (HU-22). El contenido se entrega solo a quien puede leerlo; el confidencial, solo al
/// autor y al director. Cada consulta de contenido queda en la auditoría (§25: consulta de información sensible).
/// </summary>
public class GetFollowUpQuery : IRequest<FollowUpDetailResponse>
{
    public Guid Id { get; set; }
}

/// <summary>Personas a quienes el usuario puede hacer seguimiento (§21): asistentes de sus grupos y usuarios inferiores.</summary>
public class GetFollowUpSubjectsQuery : IRequest<List<FollowUpSubject>>
{
    public string? Search { get; set; }
}

public class FollowUpQueryHandlers :
    IRequestHandler<GetFollowUpsQuery, PagedResult<FollowUpListItem>>,
    IRequestHandler<GetFollowUpQuery, FollowUpDetailResponse>,
    IRequestHandler<GetFollowUpSubjectsQuery, List<FollowUpSubject>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;
    private readonly AuditTrail _audit;

    public FollowUpQueryHandlers(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
        _audit = audit;
    }

    public async Task<PagedResult<FollowUpListItem>> Handle(GetFollowUpsQuery request, CancellationToken cancellationToken = default)
    {
        EnsurePastoralAccess();
        var directory = await _userDirectory.GetUsersAsync(cancellationToken);
        var followUps = (await _access.VisibleFollowUpsAsync(_dbContext.FollowUps, directory, cancellationToken))
            .Where(followUp => followUp.Status == request.Status);

        if (request.GroupId is not null)
        {
            followUps = followUps.InGroup(request.GroupId.Value);
        }

        if (request.WithoutDate == true)
        {
            followUps = followUps.Where(followUp => followUp.ScheduledDate == null);
        }

        if (request.From is not null)
        {
            followUps = followUps.Where(followUp => (followUp.MeetingDate ?? followUp.ScheduledDate) >= request.From);
        }

        if (request.To is not null)
        {
            followUps = followUps.Where(followUp => (followUp.MeetingDate ?? followUp.ScheduledDate) <= request.To);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpperInvariant();
            var matchingUsers = directory.Values.Where(user => user.FullName.ToUpperInvariant().Contains(search)).Select(user => user.Id).ToList();
            followUps = followUps.Where(followUp =>
                (followUp.SubjectAttendee != null && (followUp.SubjectAttendee.FirstName + " " + followUp.SubjectAttendee.LastName).ToUpper().Contains(search))
                || (followUp.SubjectUserId != null && matchingUsers.Contains(followUp.SubjectUserId.Value)));
        }

        var ordered = request.Status == FollowUpStatus.Pending
            ? followUps.OrderBy(followUp => followUp.ScheduledDate == null).ThenBy(followUp => followUp.ScheduledDate).ThenBy(followUp => followUp.CreatedDate)
            : followUps.OrderByDescending(followUp => followUp.MeetingDate).ThenByDescending(followUp => followUp.CreatedDate);

        var total = await ordered.CountAsync(cancellationToken);
        var page = await ordered.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .Include(followUp => followUp.SubjectAttendee)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var items = await ToListItemsAsync(page, directory, cancellationToken);
        return new PagedResult<FollowUpListItem>(items, total, request.PageNumber, request.PageSize);
    }

    public async Task<FollowUpDetailResponse> Handle(GetFollowUpQuery request, CancellationToken cancellationToken = default)
    {
        EnsurePastoralAccess();
        var directory = await _userDirectory.GetUsersAsync(cancellationToken);
        var followUp = await (await _access.VisibleFollowUpsAsync(_dbContext.FollowUps, directory, cancellationToken))
            .Include(item => item.SubjectAttendee)
            .Include(item => item.Topic)
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(FollowUp), request.Id);

        var item = (await ToListItemsAsync([followUp], directory, cancellationToken))[0];

        if (followUp.Status != FollowUpStatus.Done)
        {
            return new FollowUpDetailResponse(item, null, false);
        }

        if (!_access.CanReadFollowUpContent(followUp))
        {
            return new FollowUpDetailResponse(item, null, ContentProtected: true);
        }

        _audit.Record(AuditModule.FollowUps, AuditActions.SensitiveRead, followUp.Id, $"Seguimiento / {item.Subject.FullName}", null, null);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new FollowUpDetailResponse(item, new FollowUpContentResponse(
            followUp.MeetingDate!.Value, followUp.Location!, followUp.TopicId!.Value, followUp.Topic!.Name,
            followUp.Description!, followUp.Commitments!, followUp.Observations), false);
    }

    public async Task<List<FollowUpSubject>> Handle(GetFollowUpSubjectsQuery request, CancellationToken cancellationToken = default)
    {
        if (!_access.CanFollowUp)
        {
            throw new ForbiddenAccessException();
        }

        var scope = await _access.GetScopeAsync(cancellationToken);
        var directory = await _userDirectory.GetUsersAsync(cancellationToken);
        var search = request.Search?.Trim().ToUpperInvariant() ?? string.Empty;

        var attendees = _dbContext.Attendees.Where(attendee => attendee.IsActive);
        if (!scope.IsDirector)
        {
            var groupIds = scope.ActiveManagedGroupIds;
            attendees = attendees.Where(attendee => attendee.Memberships.Any(membership => membership.EndedAt == null && groupIds.Contains(membership.GroupId)));
        }

        var attendeeSubjects = await attendees
            .Where(attendee => search == string.Empty || (attendee.FirstName + " " + attendee.LastName).ToUpper().Contains(search))
            .OrderBy(attendee => attendee.FirstName).ThenBy(attendee => attendee.LastName)
            .Take(50)
            .Select(attendee => new FollowUpSubject(PersonType.Attendee, attendee.Id, attendee.FirstName + " " + attendee.LastName,
                attendee.Memberships.Where(membership => membership.EndedAt == null)
                    .Select(membership => membership.Group.Generation.Name + " · " + membership.Group.Name).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var inferior = await _access.InferiorUserIdsAsync(directory, cancellationToken);
        var userSubjects = inferior.Select(directory.GetValueOrDefault).OfType<DirectoryUser>()
            .Where(user => user.IsActive && user.FullName.ToUpperInvariant().Contains(search))
            .OrderBy(user => user.FullName)
            .Take(50)
            .Select(user => new FollowUpSubject(PersonType.User, user.Id, user.FullName, RoleLabels.Of(user.Role)));

        return [.. userSubjects, .. attendeeSubjects];
    }

    /// <summary>El aprendiz y el administrador no tienen acceso pastoral (§4.4, §4.5).</summary>
    private void EnsurePastoralAccess()
    {
        if (!_access.CanFollowUp)
        {
            throw new ForbiddenAccessException();
        }
    }

    private async Task<List<FollowUpListItem>> ToListItemsAsync(List<FollowUp> followUps, IReadOnlyDictionary<Guid, DirectoryUser> directory, CancellationToken cancellationToken)
    {
        var attendeeIds = followUps.Where(item => item.SubjectAttendeeId != null).Select(item => item.SubjectAttendeeId!.Value).Distinct().ToList();
        var groups = await _dbContext.GroupMemberships
            .Where(membership => membership.EndedAt == null && attendeeIds.Contains(membership.AttendeeId))
            .Select(membership => new { membership.AttendeeId, Name = membership.Group.Generation.Name + " · " + membership.Group.Name })
            .ToDictionaryAsync(item => item.AttendeeId, item => item.Name, cancellationToken);
        // Los realizados muestran el grupo de la reunión, no el actual (HU-06).
        var meetingGroupIds = followUps.Where(item => item.GroupId != null).Select(item => item.GroupId!.Value).Distinct().ToList();
        var meetingGroups = await _dbContext.Groups.IgnoreQueryFilters()
            .Where(group => meetingGroupIds.Contains(group.Id))
            .Select(group => new { group.Id, Name = group.Generation.Name + " · " + group.Name })
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);

        string? GroupLabel(FollowUp followUp, Guid attendeeId) =>
            followUp.GroupId is { } groupId ? meetingGroups.GetValueOrDefault(groupId) : groups.GetValueOrDefault(attendeeId);

        return followUps.Select(followUp =>
        {
            var subject = followUp.SubjectAttendee is { } attendee
                ? new FollowUpSubject(PersonType.Attendee, attendee.Id, attendee.FullName, GroupLabel(followUp, attendee.Id))
                : new FollowUpSubject(PersonType.User, followUp.SubjectUserId!.Value, directory.NameOf(followUp.SubjectUserId.Value),
                    RoleLabels.Of(directory.GetValueOrDefault(followUp.SubjectUserId.Value)?.Role));

            return new FollowUpListItem(followUp.Id, subject, followUp.AuthorUserId, directory.NameOf(followUp.CreatedBy), followUp.Status,
                followUp.ScheduledDate, followUp.ScheduledTime, followUp.MeetingDate, followUp.IsConfidential, _access.CanEditFollowUp(followUp));
        }).ToList();
    }
}

/// <summary>Nombres de los roles en la interfaz.</summary>
public static class RoleLabels
{
    public static string? Of(UserRole? role) => role switch
    {
        UserRole.Director => "Director",
        UserRole.Coordinator => "Coordinador",
        UserRole.Leader => "Líder",
        UserRole.Apprentice => "Aprendiz",
        UserRole.Admin => "Administrador",
        _ => null
    };
}

public class GetFollowUpsQueryValidator : AbstractValidator<GetFollowUpsQuery>
{
    public GetFollowUpsQueryValidator()
    {
        RuleFor(query => query.Status).IsInEnum();
        RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Search).MaximumLength(100);
    }
}
