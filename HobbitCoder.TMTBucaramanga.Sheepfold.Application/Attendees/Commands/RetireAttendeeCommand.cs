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

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Commands;

/// <summary>Retira la ficha de las consultas habituales. Solo el director.</summary>
public class RetireAttendeeCommand : IRequest
{
    public Guid Id { get; set; }
}

public class RetireAttendeeCommandHandler : IRequestHandler<RetireAttendeeCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;

    public RetireAttendeeCommandHandler(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
    }

    public async Task<Unit> Handle(RetireAttendeeCommand request, CancellationToken cancellationToken = default)
    {
        _access.EnsureDirector();

        var attendee = await _dbContext.Attendees.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Attendee), request.Id);

        _dbContext.Attendees.Remove(attendee);
        _audit.Record(AuditModule.Attendees, AuditActions.Retired, attendee.Id, attendee.FullName, AttendeeCommandHelpers.Snapshot(attendee), null);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
