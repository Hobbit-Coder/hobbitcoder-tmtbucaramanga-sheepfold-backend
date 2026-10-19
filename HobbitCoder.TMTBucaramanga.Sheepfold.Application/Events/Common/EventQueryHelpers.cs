using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using System.Linq.Expressions;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

public static class EventQueryHelpers
{
    /// <summary>
    /// Filtros de fecha, tipo, generación y grupo. Se aplican sobre los eventos visibles: nunca amplían el
    /// alcance. Un evento de grupos coincide con una generación si alguno de sus grupos es de ella.
    /// </summary>
    public static IQueryable<Event> Filter(IQueryable<Event> events, DateTimeOffset? from, DateTimeOffset? to, Guid? eventTypeId, Guid? generationId, Guid? groupId)
    {
        if (from is not null)
        {
            var fromUtc = from.Value.UtcDateTime;
            events = events.Where(@event => @event.EndsAt >= fromUtc);
        }

        if (to is not null)
        {
            var toUtc = to.Value.UtcDateTime;
            events = events.Where(@event => @event.StartsAt < toUtc);
        }

        if (eventTypeId is not null)
        {
            events = events.Where(@event => @event.EventTypeId == eventTypeId);
        }

        if (generationId is not null)
        {
            events = events.Where(@event => @event.Generations.Any(item => item.GenerationId == generationId)
                || @event.Groups.Any(item => item.Group.GenerationId == generationId));
        }

        if (groupId is not null)
        {
            events = events.Where(@event => @event.Groups.Any(item => item.GroupId == groupId));
        }

        return events;
    }

    /// <summary>Proyección común del resumen de un evento.</summary>
    public static readonly Expression<Func<Event, EventSummaryResponse>> ToSummary = @event => new EventSummaryResponse
    {
        Id = @event.Id,
        Title = @event.Title,
        EventType = new NamedRef { Id = @event.EventTypeId, Name = @event.EventType.Name },
        StartsAt = @event.StartsAt,
        EndsAt = @event.EndsAt,
        Location = @event.Location,
        Audience = @event.Audience,
        Generations = @event.Generations.OrderBy(item => item.Generation.SortOrder).Select(item => new NamedRef { Id = item.GenerationId, Name = item.Generation.Name }).ToList(),
        Groups = @event.Groups.Select(item => new AudienceGroupRef { Id = item.GroupId, Name = item.Group.Name, Year = item.Group.Year, GenerationId = item.Group.GenerationId, GenerationName = item.Group.Generation.Name }).ToList(),
        Status = @event.Status,
        RequiresAttendance = @event.RequiresAttendance
    };
}
