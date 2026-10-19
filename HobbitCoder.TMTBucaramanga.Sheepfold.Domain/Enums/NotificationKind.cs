namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

/// <summary>Novedades que generan notificación.</summary>
public enum NotificationKind
{
    UpcomingEvent = 1,
    EventChanged = 2,
    EventCancelled = 3,
    Birthday = 4,
    AttendeeAssigned = 5,
    PendingFollowUp = 6,
    EventResponsibility = 7,
    DiscussionActivity = 8
}
