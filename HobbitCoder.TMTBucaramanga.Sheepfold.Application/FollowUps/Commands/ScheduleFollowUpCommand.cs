using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Commands;

/// <summary>Cambia la fecha programada de un pendiente (autor o director).</summary>
public class ScheduleFollowUpCommand : IRequest
{
    public Guid Id { get; set; }
    public DateOnly? ScheduledDate { get; set; }
    public TimeOnly? ScheduledTime { get; set; }
}

public class ScheduleFollowUpCommandHandler : IRequestHandler<ScheduleFollowUpCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;
    private readonly AuditTrail _audit;

    public ScheduleFollowUpCommandHandler(
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

    public async Task<Unit> Handle(ScheduleFollowUpCommand request, CancellationToken cancellationToken = default)
    {
        var followUp = await FollowUpCommandHelpers.FindEditableAsync(_dbContext, _access, _userDirectory, request.Id, cancellationToken);

        if (followUp.Status != FollowUpStatus.Pending)
        {
            throw new ConflictException("Only pending follow-ups have a scheduled date.");
        }

        var before = FollowUpCommandHelpers.Snapshot(followUp);
        followUp.ScheduledDate = request.ScheduledDate;
        followUp.ScheduledTime = request.ScheduledDate is null ? null : request.ScheduledTime;

        _audit.Record(AuditModule.FollowUps, AuditActions.Updated, followUp.Id, "Seguimiento", before, FollowUpCommandHelpers.Snapshot(followUp));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
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
