using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Commands;

/// <summary>Retira el grupo de las consultas habituales. Solo el director, y sin asistentes vigentes.</summary>
public class RetireGroupCommand : IRequest
{
    public Guid Id { get; set; }
}

public class RetireGroupCommandHandler : IRequestHandler<RetireGroupCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;

    public RetireGroupCommandHandler(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
    }

    public async Task<Unit> Handle(RetireGroupCommand request, CancellationToken cancellationToken = default)
    {
        _access.EnsureDirector();

        var group = await _dbContext.Groups.Include(item => item.Generation).FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Group), request.Id);
        await GroupCommandHelpers.EnsureNoCurrentMembersAsync(_dbContext, group.Id, cancellationToken);

        _dbContext.Groups.Remove(group);
        _audit.Record(AuditModule.Groups, AuditActions.Retired, group.Id, GroupCommandHelpers.Describe(group, group.Generation), new { group.Name, group.IsActive }, null);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
