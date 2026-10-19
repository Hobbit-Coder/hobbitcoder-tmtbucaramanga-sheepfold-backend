using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Calendar.Common;

public sealed class CalendarResponse
{
    public required IReadOnlyList<EventSummaryResponse> Events { get; set; }
    public required IReadOnlyList<BirthdayItem> Birthdays { get; set; }
    public required IReadOnlyList<CalendarFollowUpItem> FollowUps { get; set; }
    public int? PendingWithoutDate { get; set; }
}
