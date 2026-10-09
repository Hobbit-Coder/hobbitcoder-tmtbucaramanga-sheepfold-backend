using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Contexts;

public class SheepfoldContext : DbContext, IApplicationDbContext
{
    private const int AuditUserMaxLength = 256;
    private const int EnumMaxLength = 40;

    public SheepfoldContext(DbContextOptions<SheepfoldContext> options) : base(options)
    {
    }

    public DbSet<Generation> Generations => Set<Generation>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<FollowUpTopic> FollowUpTopics => Set<FollowUpTopic>();
    public DbSet<Course> Courses => Set<Course>();

    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupAssignment> GroupAssignments => Set<GroupAssignment>();
    public DbSet<GroupMembership> GroupMemberships => Set<GroupMembership>();

    public DbSet<Attendee> Attendees => Set<Attendee>();
    public DbSet<AttendeeContact> AttendeeContacts => Set<AttendeeContact>();
    public DbSet<AttendeeCourse> AttendeeCourses => Set<AttendeeCourse>();

    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventGeneration> EventGenerations => Set<EventGeneration>();
    public DbSet<EventGroup> EventGroups => Set<EventGroup>();
    public DbSet<EventManager> EventManagers => Set<EventManager>();
    public DbSet<EventAttachment> EventAttachments => Set<EventAttachment>();
    public DbSet<EventComment> EventComments => Set<EventComment>();
    public DbSet<EventCommentAttachment> EventCommentAttachments => Set<EventCommentAttachment>();
    public DbSet<AttendeeAttendance> AttendeeAttendances => Set<AttendeeAttendance>();
    public DbSet<UserAttendance> UserAttendances => Set<UserAttendance>();

    public DbSet<FollowUp> FollowUps => Set<FollowUp>();

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationChannelSetting> NotificationChannelSettings => Set<NotificationChannelSetting>();
    public DbSet<NotificationKindSetting> NotificationKindSettings => Set<NotificationKindSetting>();
    public DbSet<OperationalSettings> OperationalSettings => Set<OperationalSettings>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    /// <summary>Los enums se guardan por nombre: la base se lee sin tablas de códigos.</summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(EnumMaxLength);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PersistenceConstants.Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SheepfoldContext).Assembly);

        ApplyAuditableConventions(modelBuilder);
        ApplySoftDeleteQueryFilters(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void ApplyAuditableConventions(ModelBuilder modelBuilder)
    {
        foreach (var clrType in GetEntityClrTypes<IAuditableEntity>(modelBuilder))
        {
            var entity = modelBuilder.Entity(clrType);

            entity.Property(nameof(IAuditableEntity.CreatedBy)).HasMaxLength(AuditUserMaxLength);
            entity.Property(nameof(IAuditableEntity.UpdatedBy)).HasMaxLength(AuditUserMaxLength);

            if (typeof(ISoftDeleteableEntity).IsAssignableFrom(clrType))
            {
                entity.Property(nameof(ISoftDeleteableEntity.DeletedBy)).HasMaxLength(AuditUserMaxLength);
            }
        }
    }

    /// <summary>
    /// Oculta los registros borrados lógicamente en todas las consultas. Para verlos,
    /// una consulta debe pedirlo explícitamente con <c>IgnoreQueryFilters()</c>.
    /// </summary>
    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var clrType in GetEntityClrTypes<ISoftDeleteableEntity>(modelBuilder))
        {
            var entity = Expression.Parameter(clrType, "entity");
            var isNotDeleted = Expression.Not(Expression.Property(entity, nameof(ISoftDeleteableEntity.IsDeleted)));

            modelBuilder.Entity(clrType).HasQueryFilter(Expression.Lambda(isNotDeleted, entity));
        }
    }

    private static List<Type> GetEntityClrTypes<TContract>(ModelBuilder modelBuilder) =>
        modelBuilder.Model.GetEntityTypes()
            .Select(entityType => entityType.ClrType)
            .Where(clrType => typeof(TContract).IsAssignableFrom(clrType))
            .ToList();
}
