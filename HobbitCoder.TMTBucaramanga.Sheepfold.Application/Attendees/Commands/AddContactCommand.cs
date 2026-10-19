using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Commands;

/// <summary>Agrega un tutor o contacto de emergencia.</summary>
public class AddContactCommand : IRequest<Guid>
{
    public Guid AttendeeId { get; set; }
    public required ContactData Contact { get; set; }
}

public class AddContactCommandHandler : IRequestHandler<AddContactCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;

    public AddContactCommandHandler(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
    }

    public async Task<Guid> Handle(AddContactCommand request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.AttendeeId, cancellationToken);
        var contact = AttendeeCommandHelpers.ToEntity(attendee.Id, request.Contact);

        _dbContext.AttendeeContacts.Add(contact);
        _audit.Record(AuditModule.Attendees, AuditActions.ContactAdded, attendee.Id, attendee.FullName, null, request.Contact);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return contact.Id;
    }
}

public class AddContactCommandValidator : AbstractValidator<AddContactCommand>
{
    public AddContactCommandValidator()
    {
        RuleFor(command => command.Contact).NotNull().SetValidator(new ContactDataValidator());
    }
}
