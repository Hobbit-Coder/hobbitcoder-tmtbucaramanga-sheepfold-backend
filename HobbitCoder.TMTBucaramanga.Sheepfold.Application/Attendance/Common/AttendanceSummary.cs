namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Common;

public sealed class AttendanceSummary
{
    public required int Attended { get; set; }
    public required int Late { get; set; }

    /// <summary>Personas sin marca: se interpretan como no asistió.</summary>
    public required int Unmarked { get; set; }
}
