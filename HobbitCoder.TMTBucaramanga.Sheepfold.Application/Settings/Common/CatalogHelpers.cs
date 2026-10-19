using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Common;

public static class CatalogHelpers
{
    public static CatalogSnapshot Snapshot(CatalogItem item) =>
        new CatalogSnapshot { Name = item.Name, IsActive = item.IsActive, Profile = (item as Generation)?.Profile, SortOrder = (item as Generation)?.SortOrder };

    public static Task<bool> NameExistsAsync(IApplicationDbContext dbContext, CatalogKind kind, string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var normalized = name.ToUpperInvariant();

        return kind switch
        {
            CatalogKind.Generations => dbContext.Generations.AnyAsync(item => item.Id != exceptId && item.Name.ToUpper() == normalized, cancellationToken),
            CatalogKind.EventTypes => dbContext.EventTypes.AnyAsync(item => item.Id != exceptId && item.Name.ToUpper() == normalized, cancellationToken),
            CatalogKind.FollowUpTopics => dbContext.FollowUpTopics.AnyAsync(item => item.Id != exceptId && item.Name.ToUpper() == normalized, cancellationToken),
            _ => dbContext.Courses.AnyAsync(item => item.Id != exceptId && item.Name.ToUpper() == normalized, cancellationToken)
        };
    }
}
