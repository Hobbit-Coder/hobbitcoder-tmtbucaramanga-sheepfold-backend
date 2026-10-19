using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

public static class EventTransitions
{
    /// <summary>
    /// Cambios de estado admitidos. Finalizar no cierra la asistencia; cancelar conserva datos y
    /// conversación. Un cancelado o finalizado puede volver a publicarse.
    /// </summary>
    public static IReadOnlyList<EventStatus> From(EventStatus status) => status switch
    {
        EventStatus.Draft => [EventStatus.Published, EventStatus.Cancelled],
        EventStatus.Published => [EventStatus.Cancelled, EventStatus.Finished],
        EventStatus.Cancelled => [EventStatus.Published],
        _ => [EventStatus.Published]
    };
}
