using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;

public sealed class AttendanceHistoryItem
{
    public required Guid EventId { get; set; }
    public required string EventTitle { get; set; }
    public required DateTime StartsAt { get; set; }
    public required AttendanceMark Mark { get; set; }

    /// <summary>Grupo al que pertenecía en la fecha del evento, no el actual.</summary>
    public string? GroupAtTheTime { get; set; }
}
