using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Commands;

/// <summary>Retira el evento de las consultas habituales; conserva asistencia, conversación y archivos.</summary>
public class RetireEventCommand : IRequest
{
    public Guid Id { get; set; }
}

public class RetireEventCommandHandler : IRequestHandler<RetireEventCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;

    public RetireEventCommandHandler(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
    }

    public async Task<Unit> Handle(RetireEventCommand request, CancellationToken cancellationToken = default)
    {
        var @event = await EventCommandHelpers.FindAdministrableAsync(_dbContext, _access, request.Id, cancellationToken);

        _dbContext.Events.Remove(@event);
        _audit.Record(AuditModule.Events, AuditActions.Retired, @event.Id, @event.Title, EventCommandHelpers.Snapshot(@event), null);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
