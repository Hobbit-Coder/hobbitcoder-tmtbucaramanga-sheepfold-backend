using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Audit.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Audit.Queries;

public class GetAuditEntryQuery : IRequest<AuditEntryResponse>
{
    public Guid Id { get; set; }
}

public class GetAuditEntryQueryHandler : IRequestHandler<GetAuditEntryQuery, AuditEntryResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;

    public GetAuditEntryQueryHandler(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
    }

    public async Task<AuditEntryResponse> Handle(GetAuditEntryQuery request, CancellationToken cancellationToken = default)
    {
        _access.EnsureDirector();

        var entry = await _dbContext.AuditEntries.AsNoTracking().FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(AuditEntry), request.Id);

        return AuditQueryHelpers.ToResponse(entry, await _userDirectory.GetUsersAsync(cancellationToken));
    }
}
