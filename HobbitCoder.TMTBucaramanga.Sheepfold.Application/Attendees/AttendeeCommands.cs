using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;
using ValidationException = HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees;

/// <summary>
/// Registra un asistente (§7, HU-08): el director siempre; los demás solo con la designación de Identity, que
/// no amplía lo que pueden consultar o editar después. Queda en un grupo activo desde el principio y con el
/// contacto que exige su generación (§9).
/// </summary>
public class CreateAttendeeCommand : IRequest<Guid>, IAttendeeData
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public Gender? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Instagram { get; set; }
    public DateOnly? BirthDate { get; set; }
    public EducationalStage? EducationalStage { get; set; }
    public bool IsServer { get; set; }
    public Guid GroupId { get; set; }
    public List<ContactData> Contacts { get; set; } = [];
}

/// <summary>Edita los datos de la ficha dentro del alcance (§4): director, coordinador, líder y aprendiz.</summary>
public class UpdateAttendeeCommand : IRequest, IAttendeeData
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public Gender? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Instagram { get; set; }
    public DateOnly? BirthDate { get; set; }
    public EducationalStage? EducationalStage { get; set; }
    public bool IsServer { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Traslado a otro grupo activo (§13, HU-06). Sin motivo ni fecha manual. Si la nueva generación exige otro tipo
/// de contacto que el asistente no tiene (por ejemplo, de PULSO a ECO), se debe agregar en el mismo traslado.
/// </summary>
public class TransferAttendeeCommand : IRequest
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public ContactData? Contact { get; set; }
}

/// <summary>Retira la ficha de las consultas habituales (§26). Solo el director.</summary>
public class RetireAttendeeCommand : IRequest
{
    public Guid Id { get; set; }
}

public class AttendeeCommandHandlers :
    IRequestHandler<CreateAttendeeCommand, Guid>,
    IRequestHandler<UpdateAttendeeCommand, Unit>,
    IRequestHandler<TransferAttendeeCommand, Unit>,
    IRequestHandler<RetireAttendeeCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly MembershipService _memberships;
    private readonly AuditTrail _audit;

    public AttendeeCommandHandlers(IApplicationDbContext dbContext, MinistryAccess access, MembershipService memberships, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _memberships = memberships;
        _audit = audit;
    }

    public async Task<Guid> Handle(CreateAttendeeCommand request, CancellationToken cancellationToken = default)
    {
        _access.EnsureMinistry();

        if (!_access.CanRegisterAttendees)
        {
            throw new ForbiddenAccessException();
        }

        var group = await _memberships.FindActiveGroupAsync(request.GroupId, cancellationToken);
        EnsureRequiredContact(group.Generation, request.Contacts.Select(contact => contact.Kind));

        var attendee = new Attendee { FirstName = request.FirstName, LastName = request.LastName };
        Apply(attendee, request);
        _dbContext.Attendees.Add(attendee);
        _memberships.AddInitialMembership(attendee, group);
        _dbContext.AttendeeContacts.AddRange(request.Contacts.Select(contact => ToEntity(attendee.Id, contact)));

        _audit.Record(AuditModule.Attendees, AuditActions.Created, attendee.Id, attendee.FullName, null,
            new { Data = Snapshot(attendee), Group = MembershipService.Describe(group), Contacts = request.Contacts.Count });
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _memberships.NotifyAssignedAsync(attendee, group, cancellationToken);

        return attendee.Id;
    }

    public async Task<Unit> Handle(UpdateAttendeeCommand request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.Id, cancellationToken);

        var before = Snapshot(attendee);
        Apply(attendee, request);
        attendee.IsActive = request.IsActive;

        _audit.Record(AuditModule.Attendees, AuditActions.ForActivation(before.IsActive, attendee.IsActive), attendee.Id, attendee.FullName, before, Snapshot(attendee));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    public async Task<Unit> Handle(TransferAttendeeCommand request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.Id, cancellationToken);
        var scope = await _access.GetScopeAsync(cancellationToken);

        // Trasladan el director y, entre grupos de su alcance, el coordinador o el líder; el aprendiz no.
        if (!scope.IsDirector && (scope.Role is not (UserRole.Coordinator or UserRole.Leader) || !scope.ActiveManagedGroupIds.Contains(request.GroupId)))
        {
            throw new ForbiddenAccessException();
        }

        var destination = await _memberships.FindActiveGroupAsync(request.GroupId, cancellationToken);
        var currentKinds = await _dbContext.AttendeeContacts.Where(contact => contact.AttendeeId == attendee.Id).Select(contact => contact.Kind).ToListAsync(cancellationToken);
        EnsureRequiredContact(destination.Generation, request.Contact is null ? currentKinds : [.. currentKinds, request.Contact.Kind]);

        var transferred = await _memberships.TransferAsync(attendee, destination, request.Contact is null ? null : token =>
        {
            _dbContext.AttendeeContacts.Add(ToEntity(attendee.Id, request.Contact));
            _audit.Record(AuditModule.Attendees, AuditActions.ContactAdded, attendee.Id, attendee.FullName, null, request.Contact);
            return Task.CompletedTask;
        }, cancellationToken);

        if (transferred)
        {
            await _memberships.NotifyAssignedAsync(attendee, destination, cancellationToken);
        }

        return Unit.Value;
    }

    public async Task<Unit> Handle(RetireAttendeeCommand request, CancellationToken cancellationToken = default)
    {
        _access.EnsureDirector();

        var attendee = await _dbContext.Attendees.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Attendee), request.Id);

        _dbContext.Attendees.Remove(attendee);
        _audit.Record(AuditModule.Attendees, AuditActions.Retired, attendee.Id, attendee.FullName, Snapshot(attendee), null);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    /// <summary>§9: PULSO exige al menos un tutor legal; ECO y LEGADO, al menos un contacto de emergencia.</summary>
    internal static void EnsureRequiredContact(Generation generation, IEnumerable<ContactKind> kinds)
    {
        if (!kinds.Contains(generation.RequiredContactKind))
        {
            var message = generation.RequiredContactKind == ContactKind.LegalGuardian
                ? "Add at least one legal guardian."
                : "Add at least one emergency contact.";
            throw ValidationException.For("Contacts", message);
        }
    }

    internal static AttendeeContact ToEntity(Guid attendeeId, ContactData contact) => new()
    {
        AttendeeId = attendeeId,
        Kind = contact.Kind,
        FullName = contact.FullName.Trim(),
        Relationship = contact.Relationship.Trim(),
        PhoneNumber = contact.PhoneNumber.Trim()
    };

    private static void Apply(Attendee attendee, IAttendeeData data)
    {
        attendee.FirstName = data.FirstName.Trim();
        attendee.LastName = data.LastName.Trim();
        attendee.Gender = data.Gender;
        attendee.PhoneNumber = string.IsNullOrWhiteSpace(data.PhoneNumber) ? null : data.PhoneNumber.Trim();
        attendee.Instagram = string.IsNullOrWhiteSpace(data.Instagram) ? null : data.Instagram.Trim();
        attendee.BirthDate = data.BirthDate;
        attendee.EducationalStage = data.EducationalStage;
        attendee.IsServer = data.IsServer;
    }

    private static AttendeeSnapshot Snapshot(Attendee attendee) => new(
        attendee.FirstName, attendee.LastName, attendee.Gender, attendee.PhoneNumber, attendee.Instagram, attendee.BirthDate,
        attendee.EducationalStage, attendee.IsServer, attendee.IsActive);

    private sealed record AttendeeSnapshot(
        string FirstName, string LastName, Gender? Gender, string? PhoneNumber, string? Instagram, DateOnly? BirthDate,
        EducationalStage? EducationalStage, bool IsServer, bool IsActive);
}

public class CreateAttendeeCommandValidator : AbstractValidator<CreateAttendeeCommand>
{
    public CreateAttendeeCommandValidator(TimeProvider timeProvider)
    {
        Include(new AttendeeDataValidator(timeProvider));
        RuleFor(command => command.GroupId).NotEmpty().WithMessage("Select an active group.");
        RuleForEach(command => command.Contacts).SetValidator(new ContactDataValidator());
        RuleFor(command => command.Contacts.Count).LessThanOrEqualTo(20);
    }
}

public class UpdateAttendeeCommandValidator : AbstractValidator<UpdateAttendeeCommand>
{
    public UpdateAttendeeCommandValidator(TimeProvider timeProvider)
    {
        Include(new AttendeeDataValidator(timeProvider));
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
