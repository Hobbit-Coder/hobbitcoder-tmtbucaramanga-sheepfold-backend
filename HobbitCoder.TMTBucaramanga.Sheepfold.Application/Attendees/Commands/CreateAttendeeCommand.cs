using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Commands;

/// <summary>
/// Registra un asistente: el director siempre; los demás solo con la designación de Identity, que
/// no amplía lo que pueden consultar o editar después. Queda en un grupo activo desde el principio y con el
/// contacto que exige su generación.
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

public class CreateAttendeeCommandHandler : IRequestHandler<CreateAttendeeCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly MembershipService _memberships;
    private readonly AuditTrail _audit;

    public CreateAttendeeCommandHandler(
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

    public async Task<Guid> Handle(CreateAttendeeCommand request, CancellationToken cancellationToken = default)
    {
        _access.EnsureMinistry();

        if (!_access.CanRegisterAttendees)
        {
            throw new ForbiddenAccessException();
        }

        var group = await _memberships.FindActiveGroupAsync(request.GroupId, cancellationToken);
        AttendeeCommandHelpers.EnsureRequiredContact(group.Generation, request.Contacts.Select(contact => contact.Kind));

        var attendee = new Attendee { FirstName = request.FirstName, LastName = request.LastName };
        AttendeeCommandHelpers.Apply(attendee, request);
        _dbContext.Attendees.Add(attendee);
        _memberships.AddInitialMembership(attendee, group);
        _dbContext.AttendeeContacts.AddRange(request.Contacts.Select(contact => AttendeeCommandHelpers.ToEntity(attendee.Id, contact)));

        _audit.Record(AuditModule.Attendees, AuditActions.Created, attendee.Id, attendee.FullName, null,
            new { Data = AttendeeCommandHelpers.Snapshot(attendee), Group = MembershipService.Describe(group), Contacts = request.Contacts.Count });
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _memberships.NotifyAssignedAsync(attendee, group, cancellationToken);

        return attendee.Id;
    }
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
