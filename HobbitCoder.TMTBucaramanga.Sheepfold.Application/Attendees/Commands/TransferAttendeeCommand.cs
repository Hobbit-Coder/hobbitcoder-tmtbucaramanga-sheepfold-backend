using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Commands;

/// <summary>
/// Traslado a otro grupo activo. Sin motivo ni fecha indicados por el usuario. Si la nueva generación exige otro tipo
/// de contacto que el asistente no tiene (por ejemplo, de PULSO a ECO), se debe agregar en el mismo traslado.
/// </summary>
public class TransferAttendeeCommand : IRequest
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public ContactData? Contact { get; set; }
}

public class TransferAttendeeCommandHandler : IRequestHandler<TransferAttendeeCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly MembershipService _memberships;
    private readonly AuditTrail _audit;

    public TransferAttendeeCommandHandler(
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

    /// <summary>
    ///     Atiende &lt;see cref="TransferAttendeeCommand"/&gt;.
    /// </summary>
    /// <remarks>
    ///     Trasladan el director y, entre grupos de su alcance, el coordinador o el líder; el aprendiz no.
    /// </remarks>
    public async Task<Unit> Handle(TransferAttendeeCommand request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.Id, cancellationToken);
        var scope = await _access.GetScopeAsync(cancellationToken);

        if (!scope.IsDirector && (scope.Role is not (UserRole.Coordinator or UserRole.Leader) || !scope.ActiveManagedGroupIds.Contains(request.GroupId)))
        {
            throw new ForbiddenAccessException();
        }

        var destination = await _memberships.FindActiveGroupAsync(request.GroupId, cancellationToken);
        var currentKinds = await _dbContext.AttendeeContacts.Where(contact => contact.AttendeeId == attendee.Id).Select(contact => contact.Kind).ToListAsync(cancellationToken);
        AttendeeCommandHelpers.EnsureRequiredContact(destination.Generation, request.Contact is null ? currentKinds : [.. currentKinds, request.Contact.Kind]);

        var transferred = await _memberships.TransferAsync(attendee, destination, request.Contact is null ? null : token =>
        {
            _dbContext.AttendeeContacts.Add(AttendeeCommandHelpers.ToEntity(attendee.Id, request.Contact));
            _audit.Record(AuditModule.Attendees, AuditActions.ContactAdded, attendee.Id, attendee.FullName, null, request.Contact);
            return Task.CompletedTask;
        }, cancellationToken);

        if (transferred)
        {
            await _memberships.NotifyAssignedAsync(attendee, destination, cancellationToken);
        }

        return Unit.Value;
    }
}

public class TransferAttendeeCommandValidator : AbstractValidator<TransferAttendeeCommand>
{
    public TransferAttendeeCommandValidator()
    {
        RuleFor(command => command.GroupId).NotEmpty().WithMessage("Select an active group.");
        RuleFor(command => command.Contact!).SetValidator(new ContactDataValidator()).When(command => command.Contact is not null);
    }
}
