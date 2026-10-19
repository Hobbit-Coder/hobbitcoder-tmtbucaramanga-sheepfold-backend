using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>
/// Parámetros de indicadores, alertas y avisos. Son una sola fila: cada parámetro es una
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
