using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Notifications.Common;

public sealed class NotificationResponse
{
    public required Guid Id { get; set; }
    public required NotificationKind Kind { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public required DateTime CreatedAt { get; set; }
    public Guid? EventId { get; set; }
    public Guid? AttendeeId { get; set; }
}
