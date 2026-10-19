using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

/// <summary>
/// Usuarios que consultan un evento según su público, para decidir a quién notificar sus novedades.
/// Aplica las mismas reglas que <c>MinistryAccess.VisibleEventsAsync</c>, desde el lado del evento.
/// </summary>
public sealed class EventAudienceResolver
{
    private readonly IApplicationDbContext _dbContext;

    public EventAudienceResolver(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <param name="event">Evento con sus generaciones, grupos y encargados cargados.</param>
    public async Task<HashSet<Guid>> ViewerUserIdsAsync(Event @event, IReadOnlyDictionary<Guid, DirectoryUser> directory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(directory);

        var ministryUsers = directory.Values.Where(user => user.IsActive && user.IsMinistry).ToList();
        var viewers = ministryUsers.Where(user => user.Role == UserRole.Director).Select(user => user.Id).ToHashSet();
        viewers.UnionWith(@event.Managers.Select(manager => manager.UserId));

        if (Guid.TryParse(@event.CreatedBy, out var creatorId))
        {
            viewers.Add(creatorId);
        }

        if (@event.Status == EventStatus.Draft)
        {
            return viewers;
        }

        switch (@event.Audience)
        {
            case EventAudience.AllMinistry:
            case EventAudience.PrimaryGroup:
            case EventAudience.PrimaryGroupAndServers:
                viewers.UnionWith(ministryUsers.Select(user => user.Id));
                break;

            case EventAudience.DirectorsAndCoordinators:
                viewers.UnionWith(ministryUsers.Where(user => user.Role == UserRole.Coordinator).Select(user => user.Id));
                break;

            case EventAudience.DirectorsCoordinatorsAndLeaders:
                viewers.UnionWith(ministryUsers.Where(user => user.Role is UserRole.Coordinator or UserRole.Leader).Select(user => user.Id));
                break;

            case EventAudience.Generations:
                var generationIds = @event.Generations.Select(item => item.GenerationId).ToList();
                viewers.UnionWith(await _dbContext.GroupAssignments
                    .Where(assignment => assignment.Group.IsActive && generationIds.Contains(assignment.Group.GenerationId))
                    .Select(assignment => assignment.UserId)
                    .ToListAsync(cancellationToken));
                break;

            case EventAudience.Groups:
                var groupIds = @event.Groups.Select(item => item.GroupId).ToList();
                viewers.UnionWith(await _dbContext.GroupAssignments
                    .Where(assignment => groupIds.Contains(assignment.GroupId))
                    .Select(assignment => assignment.UserId)
                    .ToListAsync(cancellationToken));
                break;
        }

        return viewers;
    }
}
