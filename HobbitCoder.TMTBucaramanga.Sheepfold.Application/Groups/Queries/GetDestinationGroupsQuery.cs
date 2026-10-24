using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Queries;

/// <summary>
/// Grupos activos a los que el usuario puede registrar o trasladar asistentes: todos para el director y para
/// quien tiene la designación de registrar asistentes; los que coordina o lidera para los demás.
/// </summary>
public class GetDestinationGroupsQuery : IRequest<List<DestinationGroupResponse>>;

public class GetDestinationGroupsQueryHandler : IRequestHandler<GetDestinationGroupsQuery, List<DestinationGroupResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;

    public GetDestinationGroupsQueryHandler(IApplicationDbContext dbContext, MinistryAccess access)
    {
        _dbContext = dbContext;
        _access = access;
    }

    public async Task<List<DestinationGroupResponse>> Handle(GetDestinationGroupsQuery request, CancellationToken cancellationToken = default)
    {
        var scope = await _access.GetScopeAsync(cancellationToken);
        var groups = _dbContext.Groups.Where(group => group.IsActive);

        if (!_access.CanRegisterAttendees)
        {
            var groupIds = scope.TransferGroupIds;
            groups = groups.Where(group => groupIds.Contains(group.Id));
        }

        return await groups
            .OrderBy(group => group.Generation.SortOrder).ThenBy(group => group.Name)
            .Select(group => new DestinationGroupResponse
            {
                Id = group.Id,
                Name = group.Name,
                Year = group.Year,
                Generation = new GenerationRef { Id = group.GenerationId, Name = group.Generation.Name, Profile = group.Generation.Profile }
            })
            .ToListAsync(cancellationToken);
    }
}
