namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Common;

/// <summary>
/// Parámetros de indicadores, alertas y avisos. Un valor nulo es "No configurado":
/// el indicador muestra conteos pero no señala personas, y el aviso programado no se genera.
/// </summary>
public class OperationalSettingsDto
{
    public int? LowAttendanceThresholdPercent { get; set; }
    public int? LowAttendanceWindowDays { get; set; }
    public int? FrequentLatenessThreshold { get; set; }
    public int? FrequentLatenessWindowDays { get; set; }
    public int? UpcomingBirthdaysDays { get; set; }
    public int? UpcomingEventNoticeHours { get; set; }
    public int? PendingFollowUpReminderDays { get; set; }
}
