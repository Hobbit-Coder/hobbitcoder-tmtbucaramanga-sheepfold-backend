using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>
/// Aviso dentro de la aplicación para un usuario (§23). Solo contiene datos que su destinatario puede ver;
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

/// <summary>Canal de notificación habilitado o no en la configuración (§27).</summary>
public class NotificationChannelSetting
{
    public required NotificationChannel Channel { get; set; }
    public bool IsEnabled { get; set; }
}

/// <summary>Novedad que se notifica o no (§23, §27). Se envía por los canales habilitados.</summary>
public class NotificationKindSetting
{
    public required NotificationKind Kind { get; set; }
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Parámetros de indicadores, alertas y avisos (§27, decisión P04). Son una sola fila: cada parámetro es una
/// columna tipada. Un valor nulo significa "No configurado" y la señal o el aviso no se calcula.
/// </summary>
public class OperationalSettings : AuditableEntity
{
    public static readonly Guid SingletonId = new("00000000-0000-0000-0000-000000000001");

    /// <summary>Baja asistencia: porcentaje de participación por debajo del cual se señala a la persona.</summary>
    public int? LowAttendanceThresholdPercent { get; set; }

    /// <summary>Días hacia atrás que se miran para baja asistencia.</summary>
    public int? LowAttendanceWindowDays { get; set; }

    /// <summary>Tardanzas frecuentes: cantidad de "Llegó tarde" a partir de la cual se señala.</summary>
    public int? FrequentLatenessThreshold { get; set; }

    public int? FrequentLatenessWindowDays { get; set; }

    /// <summary>Días hacia adelante de "cumpleaños próximos" (indicador y aviso).</summary>
    public int? UpcomingBirthdaysDays { get; set; }

    /// <summary>Horas de anticipación del aviso de evento próximo.</summary>
    public int? UpcomingEventNoticeHours { get; set; }

    /// <summary>Cada cuántos días se recuerda al autor sus seguimientos pendientes.</summary>
    public int? PendingFollowUpReminderDays { get; set; }
}

/// <summary>
/// Hecho de la auditoría ministerial (§25): quién, cuándo, qué módulo y acción, el registro afectado (con su
/// referencia legible de ese momento) y la información anterior y resultante. Es de solo lectura y no tiene
/// claves foráneas para sobrevivir a la eliminación definitiva de lo auditado.
/// </summary>
public class AuditEntry : BaseEntity
{
    public required DateTime OccurredAt { get; set; }
    public required string ActorUserId { get; set; }
    public required AuditModule Module { get; set; }
    public required string Action { get; set; }
    public Guid? EntityId { get; set; }
    public required string Reference { get; set; }
    public string? Before { get; set; }
    public string? After { get; set; }
}
