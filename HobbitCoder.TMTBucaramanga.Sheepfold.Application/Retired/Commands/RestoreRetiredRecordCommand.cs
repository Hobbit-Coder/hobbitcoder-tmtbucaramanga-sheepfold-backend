using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Retired.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;
using ValidationException = HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Retired.Commands;

/// <summary>
/// Restaura un registro retirado. Un asistente vuelve a las consultas con un grupo activo: si el suyo
/// ya no lo está, se indica <see cref="GroupId"/> y se traslada.
/// </summary>
public class RestoreRetiredRecordCommand : IRequest
{
    public RetiredType Type { get; set; }
    public Guid Id { get; set; }
    public Guid? GroupId { get; set; }
}

public class RestoreRetiredRecordCommandHandler : IRequestHandler<RestoreRetiredRecordCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly MembershipService _memberships;
    private readonly AuditTrail _audit;

    public RestoreRetiredRecordCommandHandler(
        IApplicationDbContext dbContext,
        MinistryAccess access,
        MembershipService memberships,
        AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _memberships = memberships;
        _audit = audit;
    }

    public async Task<Unit> Handle(RestoreRetiredRecordCommand request, CancellationToken cancellationToken = default)
    {
        RetiredRecordHelpers.EnsureOwner(_access, request.Type);

        switch (request.Type)
        {
            case RetiredType.Attendee:
                await RestoreAttendeeAsync(request, cancellationToken);
                break;
            case RetiredType.Group:
                var group = await RetiredRecordHelpers.RetiredGroupAsync(_dbContext, request.Id, cancellationToken);
                Undelete(group);
                _audit.Record(AuditModule.Groups, AuditActions.Restored, group.Id, group.Name, null, new { group.Name, group.Year });
                break;
            case RetiredType.Event:
                var @event = await RetiredRecordHelpers.RetiredEventAsync(_dbContext, request.Id, cancellationToken);
                Undelete(@event);
                _audit.Record(AuditModule.Events, AuditActions.Restored, @event.Id, @event.Title, null, new { @event.Title, @event.Status });
                break;
            case RetiredType.FollowUp:
                var followUp = await RetiredRecordHelpers.RetiredFollowUpAsync(_dbContext, _access, request.Id, cancellationToken);
                Undelete(followUp);
                _audit.Record(AuditModule.FollowUps, AuditActions.Restored, followUp.Id, "Seguimiento", null, null);
                break;
            default:
                var item = await RetiredRecordHelpers.FindCatalogAsync(_dbContext, request.Type, request.Id, cancellationToken);
                item.IsActive = true;
                _audit.Record(AuditModule.Configuration, AuditActions.Restored, item.Id, $"{request.Type}: {item.Name}", null, new { item.Name });
                break;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private async Task RestoreAttendeeAsync(RestoreRetiredRecordCommand request, CancellationToken cancellationToken)
    {
        var attendee = await RetiredRecordHelpers.RetiredAttendeeAsync(_dbContext, request.Id, cancellationToken);
        var currentGroupActive = await _dbContext.GroupMemberships.IgnoreQueryFilters()
            .AnyAsync(membership => membership.AttendeeId == attendee.Id && membership.EndedAt == null && membership.Group.IsActive && !membership.Group.IsDeleted, cancellationToken);

        Undelete(attendee);
        _audit.Record(AuditModule.Attendees, AuditActions.Restored, attendee.Id, attendee.FullName, null, new { attendee.FirstName, attendee.LastName });

        if (currentGroupActive)
        {
            return;
        }

        if (request.GroupId is null)
        {
            throw ValidationException.For(nameof(request.GroupId), "Select an active group for the attendee.");
        }

        var group = await _memberships.FindActiveGroupAsync(request.GroupId.Value, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _memberships.TransferAsync(attendee, group, null, cancellationToken);
    }

    private static void Undelete(Domain.Entities.Common.ISoftDeleteableEntity entity)
    {
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
    }
}

public class RestoreRetiredRecordCommandValidator : AbstractValidator<RestoreRetiredRecordCommand>
{
    public RestoreRetiredRecordCommandValidator()
    {
        RuleFor(command => command.Type).IsInEnum();
        RuleFor(command => command.Id).NotEmpty();
    }
}
