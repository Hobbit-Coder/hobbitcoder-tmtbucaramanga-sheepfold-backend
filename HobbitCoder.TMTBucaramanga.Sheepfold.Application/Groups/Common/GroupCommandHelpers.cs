using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;

public static class GroupCommandHelpers
{
    /// <summary>El director, o un coordinador del grupo.</summary>
    public static async Task<Group> FindEditableAsync(IApplicationDbContext dbContext, MinistryAccess access, Guid groupId, CancellationToken cancellationToken)
    {
        var scope = await access.GetScopeAsync(cancellationToken);
        var group = await dbContext.Groups.Include(item => item.Generation).FirstOrDefaultAsync(item => item.Id == groupId, cancellationToken);

        if (group is null || (!scope.IsDirector && !scope.OperationalGroupIds.Contains(groupId)))
        {
            throw new NotFoundException(nameof(Group), groupId);
        }

        if (!scope.IsDirector && !scope.CoordinatedGroupIds.Contains(groupId))
        {
            throw new ForbiddenAccessException();
        }

        return group;
    }

    public static string Describe(Group group, Generation generation) => $"{generation.Name} · {group.Name} · {group.Year}";

    public static async Task EnsureNoCurrentMembersAsync(IApplicationDbContext dbContext, Guid groupId, CancellationToken cancellationToken)
    {
        var members = await dbContext.GroupMemberships.CountAsync(membership => membership.GroupId == groupId && membership.EndedAt == null, cancellationToken);

        if (members > 0)
        {
            throw new ConflictException($"The group still has {members} current attendees. Transfer them to another active group first.");
        }
    }

    public static async Task EnsureUniqueAsync(IApplicationDbContext dbContext, int year, Guid generationId, string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var normalized = name.ToUpperInvariant();

        if (await dbContext.Groups.IgnoreQueryFilters().AnyAsync(group => group.Id != exceptId && group.Year == year
                && group.GenerationId == generationId && group.Name.ToUpper() == normalized, cancellationToken))
        {
            throw new ConflictException($"A group named '{name}' already exists for that year and generation.");
        }
    }
}
