using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Common;

public sealed class AttendanceSheetResponse
{
    public required Guid EventId { get; set; }
    public required EventStatus Status { get; set; }
    public required IReadOnlyList<AttendanceGroupOption> Groups { get; set; }
    public required IReadOnlyList<AttendancePerson> People { get; set; }
    public required AttendanceSummary Summary { get; set; }
}
