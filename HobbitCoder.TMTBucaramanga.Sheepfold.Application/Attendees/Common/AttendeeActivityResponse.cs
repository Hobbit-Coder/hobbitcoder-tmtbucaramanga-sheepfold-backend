namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;

public sealed class AttendeeActivityResponse
{
    public required IReadOnlyList<AttendanceHistoryItem> Attendance { get; set; }
    public required IReadOnlyList<FollowUpGeneralItem> FollowUps { get; set; }
}
