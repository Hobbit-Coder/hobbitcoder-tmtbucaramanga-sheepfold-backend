using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Generation> Generations { get; }
    DbSet<EventType> EventTypes { get; }
    DbSet<FollowUpTopic> FollowUpTopics { get; }
    DbSet<Course> Courses { get; }

    DbSet<Group> Groups { get; }
    DbSet<GroupAssignment> GroupAssignments { get; }
    DbSet<GroupMembership> GroupMemberships { get; }

    DbSet<Attendee> Attendees { get; }
    DbSet<AttendeeContact> AttendeeContacts { get; }
    DbSet<AttendeeCourse> AttendeeCourses { get; }

    DbSet<Event> Events { get; }
    DbSet<EventGeneration> EventGenerations { get; }
    DbSet<EventGroup> EventGroups { get; }
    DbSet<EventManager> EventManagers { get; }
    DbSet<EventAttachment> EventAttachments { get; }
    DbSet<EventComment> EventComments { get; }
    DbSet<EventCommentAttachment> EventCommentAttachments { get; }
    DbSet<AttendeeAttendance> AttendeeAttendances { get; }
    DbSet<UserAttendance> UserAttendances { get; }

    DbSet<FollowUp> FollowUps { get; }

    DbSet<Notification> Notifications { get; }
    DbSet<NotificationChannelSetting> NotificationChannelSettings { get; }
    DbSet<NotificationKindSetting> NotificationKindSettings { get; }
    DbSet<OperationalSettings> OperationalSettings { get; }
    DbSet<AuditEntry> AuditEntries { get; }

    /// <summary>Para operaciones que necesitan una transacción explícita (traslados, renovación anual).</summary>
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
