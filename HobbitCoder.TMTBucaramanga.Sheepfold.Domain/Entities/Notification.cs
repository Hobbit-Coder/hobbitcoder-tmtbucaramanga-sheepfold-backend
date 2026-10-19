using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>
/// Aviso dentro de la aplicación para un usuario. Solo contiene datos que su destinatario puede ver;
/// nunca contenido pastoral. Referencia opcionalmente el evento o el asistente que lo origina.
/// </summary>
public class Notification : BaseEntity
{
    public required Guid RecipientUserId { get; set; }
    public required NotificationKind Kind { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public required DateTime CreatedAt { get; set; }
    public Guid? EventId { get; set; }
    public Guid? AttendeeId { get; set; }

    public virtual Event? Event { get; set; }
    public virtual Attendee? Attendee { get; set; }
}
