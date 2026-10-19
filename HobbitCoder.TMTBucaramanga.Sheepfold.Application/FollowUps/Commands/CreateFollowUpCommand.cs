using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Commands;

/// <summary>
/// Registra un seguimiento: pendiente, con fecha y hora programadas opcionales y sin contenido de
/// una reunión aún no realizada; o directamente realizado con <see cref="Completion"/>.
/// </summary>
public class CreateFollowUpCommand : IRequest<Guid>
{
    public PersonType SubjectType { get; set; }
    public Guid SubjectId { get; set; }
    public DateOnly? ScheduledDate { get; set; }
    public TimeOnly? ScheduledTime { get; set; }
    public FollowUpContent? Completion { get; set; }
}

public class CreateFollowUpCommandHandler : IRequestHandler<CreateFollowUpCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;
    private readonly AuditTrail _audit;

    public CreateFollowUpCommandHandler(
        IApplicationDbContext dbContext,
        MinistryAccess access,
        IUserDirectory userDirectory,
        AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
        _audit = audit;
    }

    public async Task<Guid> Handle(CreateFollowUpCommand request, CancellationToken cancellationToken = default)
    {
        if (!_access.CanFollowUp)
        {
            throw new ForbiddenAccessException();
        }

        var subjectName = await EnsureSubjectInScopeAsync(request.SubjectType, request.SubjectId, cancellationToken);

        var followUp = new FollowUp
        {
            SubjectAttendeeId = request.SubjectType == PersonType.Attendee ? request.SubjectId : null,
            SubjectUserId = request.SubjectType == PersonType.User ? request.SubjectId : null,
            ScheduledDate = request.ScheduledDate,
            ScheduledTime = request.ScheduledDate is null ? null : request.ScheduledTime
        };

        if (request.Completion is not null)
        {
            await FollowUpCommandHelpers.ApplyContentAsync(_dbContext, followUp, request.Completion, cancellationToken);
        }

        _dbContext.FollowUps.Add(followUp);
        _audit.Record(AuditModule.FollowUps, AuditActions.Created, followUp.Id, $"Seguimiento / {subjectName}", null, FollowUpCommandHelpers.Snapshot(followUp));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return followUp.Id;
    }

    /// <summary>
    /// el director a cualquier persona inferior; el coordinador a líderes, aprendices y asistentes de sus
    /// grupos; el líder a aprendices y asistentes de su grupo. Nadie a sí mismo.
    /// </summary>
    private async Task<string> EnsureSubjectInScopeAsync(PersonType type, Guid subjectId, CancellationToken cancellationToken)
    {
        var scope = await _access.GetScopeAsync(cancellationToken);

        if (type == PersonType.User)
        {
            var directory = await _userDirectory.GetUsersAsync(cancellationToken);
            var inferior = await _access.InferiorUserIdsAsync(directory, cancellationToken);

            return inferior.Contains(subjectId) && directory.TryGetValue(subjectId, out var user)
                ? user.FullName
                : throw new ForbiddenAccessException("The person is outside your scope.");
        }

        var groupIds = scope.ActiveManagedGroupIds;
        var attendee = await _dbContext.Attendees
            .Where(item => item.Id == subjectId && (scope.IsDirector || item.Memberships.Any(membership => membership.EndedAt == null && groupIds.Contains(membership.GroupId))))
            .Select(item => new { item.FirstName, item.LastName })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenAccessException("The person is outside your scope.");

        return $"{attendee.FirstName} {attendee.LastName}";
    }
}

public class CreateFollowUpCommandValidator : AbstractValidator<CreateFollowUpCommand>
{
    public CreateFollowUpCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(command => command.SubjectType).IsInEnum();
        RuleFor(command => command.SubjectId).NotEmpty().WithMessage("Select a person.");
        RuleFor(command => command.Completion!).SetValidator(new FollowUpContentValidator(timeProvider)).When(command => command.Completion is not null);
    }
}
