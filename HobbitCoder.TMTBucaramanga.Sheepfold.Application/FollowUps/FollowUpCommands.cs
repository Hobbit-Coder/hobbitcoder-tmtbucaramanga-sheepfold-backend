using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;
using ValidationException = HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps;

/// <summary>
/// Registra un seguimiento (HU-20, HU-21): pendiente, con fecha y hora programadas opcionales y sin contenido de
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

/// <summary>Cambia la fecha programada de un pendiente (autor o director).</summary>
public class ScheduleFollowUpCommand : IRequest
{
    public Guid Id { get; set; }
    public DateOnly? ScheduledDate { get; set; }
    public TimeOnly? ScheduledTime { get; set; }
}

/// <summary>Completa el mismo pendiente con la información de la reunión: no crea otro registro (HU-21).</summary>
public class CompleteFollowUpCommand : IRequest
{
    public Guid Id { get; set; }
    public required FollowUpContent Content { get; set; }
}

/// <summary>
/// Edita el contenido de un realizado o su confidencialidad (§22): el autor o el director. Retirar la
/// confidencialidad pasa el contenido a la consulta jerárquica ordinaria y queda auditado.
/// </summary>
public class UpdateFollowUpContentCommand : IRequest
{
    public Guid Id { get; set; }
    public required FollowUpContent Content { get; set; }
}

public class RetireFollowUpCommand : IRequest
{
    public Guid Id { get; set; }
}

public class FollowUpCommandHandlers :
    IRequestHandler<CreateFollowUpCommand, Guid>,
    IRequestHandler<ScheduleFollowUpCommand, Unit>,
    IRequestHandler<CompleteFollowUpCommand, Unit>,
    IRequestHandler<UpdateFollowUpContentCommand, Unit>,
    IRequestHandler<RetireFollowUpCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;
    private readonly AuditTrail _audit;

    public FollowUpCommandHandlers(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory, AuditTrail audit)
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
            await ApplyContentAsync(followUp, request.Completion, cancellationToken);
        }

        _dbContext.FollowUps.Add(followUp);
        _audit.Record(AuditModule.FollowUps, AuditActions.Created, followUp.Id, $"Seguimiento / {subjectName}", null, Snapshot(followUp));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return followUp.Id;
    }

    public async Task<Unit> Handle(ScheduleFollowUpCommand request, CancellationToken cancellationToken = default)
    {
        var followUp = await FindEditableAsync(request.Id, cancellationToken);

        if (followUp.Status != FollowUpStatus.Pending)
        {
            throw new ConflictException("Only pending follow-ups have a scheduled date.");
        }

        var before = Snapshot(followUp);
        followUp.ScheduledDate = request.ScheduledDate;
        followUp.ScheduledTime = request.ScheduledDate is null ? null : request.ScheduledTime;

        _audit.Record(AuditModule.FollowUps, AuditActions.Updated, followUp.Id, "Seguimiento", before, Snapshot(followUp));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    public async Task<Unit> Handle(CompleteFollowUpCommand request, CancellationToken cancellationToken = default)
    {
        var followUp = await FindEditableAsync(request.Id, cancellationToken);

        if (followUp.Status != FollowUpStatus.Pending)
        {
            throw new ConflictException("The follow-up is already done.");
        }

        var before = Snapshot(followUp);
        await ApplyContentAsync(followUp, request.Content, cancellationToken);

        _audit.Record(AuditModule.FollowUps, AuditActions.Completed, followUp.Id, "Seguimiento", before, Snapshot(followUp));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    public async Task<Unit> Handle(UpdateFollowUpContentCommand request, CancellationToken cancellationToken = default)
    {
        var followUp = await FindEditableAsync(request.Id, cancellationToken);

        if (followUp.Status != FollowUpStatus.Done)
        {
            throw new ConflictException("Complete the follow-up before editing its content.");
        }

        var before = Snapshot(followUp);
        var wasConfidential = followUp.IsConfidential;
        await ApplyContentAsync(followUp, request.Content, cancellationToken);

        var action = wasConfidential == followUp.IsConfidential ? AuditActions.Updated : AuditActions.ConfidentialityChanged;
        _audit.Record(AuditModule.FollowUps, action, followUp.Id, "Seguimiento", before, Snapshot(followUp));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    public async Task<Unit> Handle(RetireFollowUpCommand request, CancellationToken cancellationToken = default)
    {
        var followUp = await FindEditableAsync(request.Id, cancellationToken);

        _dbContext.FollowUps.Remove(followUp);
        _audit.Record(AuditModule.FollowUps, AuditActions.Retired, followUp.Id, "Seguimiento", Snapshot(followUp), null);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    /// <summary>
    /// §21: el director a cualquier persona inferior; el coordinador a líderes, aprendices y asistentes de sus
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

    private async Task<FollowUp> FindEditableAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!_access.CanFollowUp)
        {
            throw new ForbiddenAccessException();
        }

        var directory = await _userDirectory.GetUsersAsync(cancellationToken);
        var followUp = await (await _access.VisibleFollowUpsAsync(_dbContext.FollowUps, directory, cancellationToken))
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(FollowUp), id);

        return _access.CanEditFollowUp(followUp) ? followUp : throw new ForbiddenAccessException();
    }

    private async Task ApplyContentAsync(FollowUp followUp, FollowUpContent content, CancellationToken cancellationToken)
    {
        var topicIsValid = await _dbContext.FollowUpTopics.AnyAsync(topic => topic.Id == content.TopicId && (topic.IsActive || topic.Id == followUp.TopicId), cancellationToken);

        if (!topicIsValid)
        {
            throw ValidationException.For("TopicId", "Select a topic from the list.");
        }

        followUp.Status = FollowUpStatus.Done;
        followUp.MeetingDate = content.MeetingDate;
        followUp.Location = content.Location.Trim();
        followUp.TopicId = content.TopicId;
        followUp.Description = content.Description.Trim();
        followUp.Commitments = content.Commitments.Trim();
        followUp.Observations = string.IsNullOrWhiteSpace(content.Observations) ? null : content.Observations.Trim();
        followUp.IsConfidential = content.IsConfidential;

        if (followUp.SubjectAttendeeId is { } attendeeId)
        {
            followUp.GroupId = await GroupOnAsync(attendeeId, content.MeetingDate, cancellationToken);
        }
    }

    /// <summary>
    /// Grupo del asistente el día <paramref name="date"/> (hora de Bogotá; si cambió ese día, el último). Si la
    /// reunión es anterior a su primera asignación, el grupo con el que se registró.
    /// </summary>
    private async Task<Guid?> GroupOnAsync(Guid attendeeId, DateOnly date, CancellationToken cancellationToken)
    {
        var endOfDay = LocalCalendar.StartOfDayUtc(date.AddDays(1)).AddTicks(-1);
        var memberships = await _dbContext.GroupMemberships
            .Where(membership => membership.AttendeeId == attendeeId)
            .OrderBy(membership => membership.StartedAt)
            .ToListAsync(cancellationToken);

        return (memberships.LastOrDefault(membership => membership.WasActiveAt(endOfDay)) ?? memberships.FirstOrDefault())?.GroupId;
    }

    private static object Snapshot(FollowUp followUp) => new
    {
        followUp.Status,
        followUp.ScheduledDate,
        followUp.ScheduledTime,
        followUp.MeetingDate,
        followUp.Location,
        followUp.TopicId,
        followUp.Description,
        followUp.Commitments,
        followUp.Observations,
        followUp.IsConfidential
    };
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

public class CompleteFollowUpCommandValidator : AbstractValidator<CompleteFollowUpCommand>
{
    public CompleteFollowUpCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(command => command.Content).NotNull().SetValidator(new FollowUpContentValidator(timeProvider));
    }
}

public class UpdateFollowUpContentCommandValidator : AbstractValidator<UpdateFollowUpContentCommand>
{
    public UpdateFollowUpContentCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(command => command.Content).NotNull().SetValidator(new FollowUpContentValidator(timeProvider));
    }
}

/// <summary>La fecha programada se escribe en calendario local; una pasada no tiene sentido para un pendiente nuevo.</summary>
public class ScheduleFollowUpCommandValidator : AbstractValidator<ScheduleFollowUpCommand>
{
    public ScheduleFollowUpCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(command => command.ScheduledDate)
            .GreaterThanOrEqualTo(_ => LocalCalendar.Today(timeProvider).AddYears(-1)).WithMessage("ScheduledDate is not valid");
    }
}
