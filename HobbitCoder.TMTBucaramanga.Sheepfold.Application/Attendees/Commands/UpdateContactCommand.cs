using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Commands;

public class UpdateContactCommand : IRequest
{
    public Guid AttendeeId { get; set; }
    public Guid ContactId { get; set; }
    public required ContactData Contact { get; set; }
}

public class UpdateContactCommandHandler : IRequestHandler<UpdateContactCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;

    public UpdateContactCommandHandler(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
    }

    public async Task<Unit> Handle(UpdateContactCommand request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.AttendeeId, cancellationToken);
        var contact = await ContactAndCourseHelpers.FindContactAsync(_dbContext, attendee.Id, request.ContactId, cancellationToken);

        if (contact.Kind != request.Contact.Kind)
        {
            await ContactAndCourseHelpers.EnsureRequirementKeptAsync(_dbContext, attendee.Id, contact, cancellationToken);
        }

        var before = ContactAndCourseHelpers.Snapshot(contact);
        contact.Kind = request.Contact.Kind;
        contact.FullName = request.Contact.FullName.Trim();
        contact.Relationship = request.Contact.Relationship.Trim();
        contact.PhoneNumber = request.Contact.PhoneNumber.Trim();

        _audit.Record(AuditModule.Attendees, AuditActions.ContactUpdated, attendee.Id, attendee.FullName, before, ContactAndCourseHelpers.Snapshot(contact));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public class UpdateContactCommandValidator : AbstractValidator<UpdateContactCommand>
{
    public UpdateContactCommandValidator()
    {
        RuleFor(command => command.Contact).NotNull().SetValidator(new ContactDataValidator());
    }
}
