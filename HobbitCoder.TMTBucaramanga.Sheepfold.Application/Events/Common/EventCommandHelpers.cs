using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Notifications.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using ValidationException = HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

public static class EventCommandHelpers
{
    /// <summary>
    ///     Valida los datos del evento contra los catálogos y las reglas de negocio.
    /// </summary>
    /// <remarks>
    ///     Un tipo retirado solo se acepta si el evento ya lo tenía.
    /// </remarks>
    public static async Task ValidateAsync(IApplicationDbContext dbContext, EventOptionsService options, EventData request, Event? existing, CancellationToken cancellationToken)
    {
        var currentTypeId = existing?.EventTypeId;

        if (!await dbContext.EventTypes.AnyAsync(type => type.Id == request.EventTypeId && (type.IsActive || type.Id == currentTypeId), cancellationToken))
        {
            throw ValidationException.For(nameof(request.EventTypeId), "Select an event type.");
        }

        var problem = await options.ValidateAsync(request.Audience, request.GenerationIds, request.GroupIds, request.ManagerIds, existing, cancellationToken);

        if (problem is not null)
        {
            throw ValidationException.For(nameof(request.Audience), problem);
        }
    }

    public static void Sync<T>(ICollection<T> items, HashSet<Guid> wanted, Func<T, Guid> key, Func<Guid, T> create)
    {
        foreach (var item in items.Where(item => !wanted.Contains(key(item))).ToList())
        {
            items.Remove(item);
        }

        foreach (var id in wanted.Where(id => items.All(item => key(item) != id)))
        {
            items.Add(create(id));
        }
    }

    /// <summary>
    /// Sincroniza las asociaciones con lo pedido: conserva las que siguen, quita las que sobran y agrega las
    /// nuevas. Solo se guardan las generaciones o los grupos que corresponden al público elegido: "Por
    /// grupos" es el público de grupos.
    /// </summary>
    public static void Apply(Event @event, EventData request)
    {
        @event.RequiresAttendance = request.RequiresAttendance;

        var generationIds = request.Audience == EventAudience.Generations ? request.GenerationIds.ToHashSet() : [];
        var groupIds = request.Audience == EventAudience.Groups ? request.GroupIds.ToHashSet() : [];
        var managerIds = request.ManagerIds.ToHashSet();

        Sync(@event.Generations, generationIds, item => item.GenerationId, id => new EventGeneration { EventId = @event.Id, GenerationId = id });
        Sync(@event.Groups, groupIds, item => item.GroupId, id => new EventGroup { EventId = @event.Id, GroupId = id });
        Sync(@event.Managers, managerIds, item => item.UserId, id => new EventManager { EventId = @event.Id, UserId = id });
    }

    public static async Task<Event> FindAdministrableAsync(IApplicationDbContext dbContext, MinistryAccess access, Guid id, CancellationToken cancellationToken)
    {
        var visible = await access.VisibleEventsAsync(dbContext.Events, cancellationToken);
        var @event = await visible
            .Include(item => item.Managers)
            .Include(item => item.Generations)
            .Include(item => item.Groups)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Event), id);

        if (!access.CanAdministerEvent(@event))
        {
            throw new ForbiddenAccessException();
        }

        return @event;
    }

    public static object Snapshot(Event @event) => new
    {
        @event.Title,
        @event.EventTypeId,
        @event.StartsAt,
        @event.EndsAt,
        @event.Location,
        @event.Description,
        @event.Audience,
        Generations = @event.Generations.Select(item => item.GenerationId).ToList(),
        Groups = @event.Groups.Select(item => item.GroupId).ToList(),
        Managers = @event.Managers.Select(item => item.UserId).ToList(),
        @event.RequiresAttendance,
        @event.Status
    };

    public static async Task NotifyViewersAsync(EventAudienceResolver audience, NotificationDispatcher notifications, IUserDirectory userDirectory, Event @event, NotificationKind kind, string title, string message, CancellationToken cancellationToken)
    {
        var directory = await userDirectory.GetUsersAsync(cancellationToken);
        var viewers = await audience.ViewerUserIdsAsync(@event, directory, cancellationToken);

        await notifications.NotifyAsync(viewers, kind, title, message, eventId: @event.Id, cancellationToken: cancellationToken);
    }

    public static Task NotifyManagersAsync(NotificationDispatcher notifications, Event @event, IEnumerable<Guid> managerIds, CancellationToken cancellationToken) =>
        notifications.NotifyAsync(managerIds, NotificationKind.EventResponsibility, $"Eres encargado de {@event.Title}",
            $"Se te asignó como encargado del evento {@event.Title}.", eventId: @event.Id, cancellationToken: cancellationToken);
}
