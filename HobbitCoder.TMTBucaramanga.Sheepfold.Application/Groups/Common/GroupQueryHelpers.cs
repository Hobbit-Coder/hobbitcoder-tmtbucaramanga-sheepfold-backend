using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;

public static class GroupQueryHelpers
{
    public static async Task<IQueryable<Group>> VisibleGroupsAsync(IApplicationDbContext dbContext, MinistryAccess access, CancellationToken cancellationToken)
    {
        var scope = await access.GetScopeAsync(cancellationToken);

        if (scope.IsDirector)
        {
            return dbContext.Groups;
        }

        var groupIds = scope.OperationalGroupIds;
        return dbContext.Groups.Where(group => groupIds.Contains(group.Id));
    }
}
