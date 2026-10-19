using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Commands;

/// <summary>Quita un contacto; no se puede quitar el último del tipo que exige la generación.</summary>
public class RemoveContactCommand : IRequest
{
    public Guid AttendeeId { get; set; }
    public Guid ContactId { get; set; }
}

public class RemoveContactCommandHandler : IRequestHandler<RemoveContactCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;

    public RemoveContactCommandHandler(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
    }

    public async Task<Unit> Handle(RemoveContactCommand request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.AttendeeId, cancellationToken);
        var contact = await ContactAndCourseHelpers.FindContactAsync(_dbContext, attendee.Id, request.ContactId, cancellationToken);
        await ContactAndCourseHelpers.EnsureRequirementKeptAsync(_dbContext, attendee.Id, contact, cancellationToken);

        _dbContext.AttendeeContacts.Remove(contact);
        _audit.Record(AuditModule.Attendees, AuditActions.ContactRemoved, attendee.Id, attendee.FullName, ContactAndCourseHelpers.Snapshot(contact), null);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
