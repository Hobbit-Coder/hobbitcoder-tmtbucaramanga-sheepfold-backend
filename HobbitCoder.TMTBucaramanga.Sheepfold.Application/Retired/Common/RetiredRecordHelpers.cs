using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Retired.Common;

public static class RetiredRecordHelpers
{
    public static async Task<Group> RetiredGroupAsync(IApplicationDbContext dbContext, Guid id, CancellationToken cancellationToken) =>
        await dbContext.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted, cancellationToken)
        ?? throw new NotFoundException(nameof(Group), id);

    public static async Task<Attendee> RetiredAttendeeAsync(IApplicationDbContext dbContext, Guid id, CancellationToken cancellationToken) =>
        await dbContext.Attendees.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted, cancellationToken)
        ?? throw new NotFoundException(nameof(Attendee), id);

    public static async Task<CatalogItem> FindCatalogAsync(IApplicationDbContext dbContext, RetiredType type, Guid id, CancellationToken cancellationToken)
    {
        CatalogItem? item = type switch
        {
            RetiredType.Generation => await dbContext.Generations.FirstOrDefaultAsync(entity => entity.Id == id && !entity.IsActive, cancellationToken),
            RetiredType.EventType => await dbContext.EventTypes.FirstOrDefaultAsync(entity => entity.Id == id && !entity.IsActive, cancellationToken),
            RetiredType.FollowUpTopic => await dbContext.FollowUpTopics.FirstOrDefaultAsync(entity => entity.Id == id && !entity.IsActive, cancellationToken),
            _ => await dbContext.Courses.FirstOrDefaultAsync(entity => entity.Id == id && !entity.IsActive, cancellationToken)
        };

        return item ?? throw new NotFoundException(type.ToString(), id);
    }

    /// <summary>El director nunca recibe un seguimiento del que es la persona atendida.</summary>
    public static async Task<FollowUp> RetiredFollowUpAsync(IApplicationDbContext dbContext, MinistryAccess access, Guid id, CancellationToken cancellationToken)
    {
        var me = access.UserId;
        return await dbContext.FollowUps.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted && item.SubjectUserId != me, cancellationToken)
            ?? throw new NotFoundException(nameof(FollowUp), id);
    }

    public static async Task<Event> RetiredEventAsync(IApplicationDbContext dbContext, Guid id, CancellationToken cancellationToken) =>
        await dbContext.Events.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted, cancellationToken)
        ?? throw new NotFoundException(nameof(Event), id);

    public static void EnsureOwner(MinistryAccess access, RetiredType type)
    {
        var isCatalog = type >= RetiredType.Generation;

        if (isCatalog ? !access.IsAdmin : !access.IsDirector)
        {
            throw new ForbiddenAccessException();
        }
    }
}
