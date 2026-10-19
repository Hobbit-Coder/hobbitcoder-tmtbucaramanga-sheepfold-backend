using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Commands;

public class RetireFollowUpCommand : IRequest
{
    public Guid Id { get; set; }
}

public class RetireFollowUpCommandHandler : IRequestHandler<RetireFollowUpCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;
    private readonly AuditTrail _audit;

    public RetireFollowUpCommandHandler(
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

    public async Task<Unit> Handle(RetireFollowUpCommand request, CancellationToken cancellationToken = default)
    {
        var followUp = await FollowUpCommandHelpers.FindEditableAsync(_dbContext, _access, _userDirectory, request.Id, cancellationToken);

        _dbContext.FollowUps.Remove(followUp);
        _audit.Record(AuditModule.FollowUps, AuditActions.Retired, followUp.Id, "Seguimiento", FollowUpCommandHelpers.Snapshot(followUp), null);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
