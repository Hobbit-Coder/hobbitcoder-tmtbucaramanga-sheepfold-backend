using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.Property(@event => @event.Title).HasMaxLength(150).IsRequired();
        builder.Property(@event => @event.Location).HasMaxLength(150).IsRequired();
        builder.Property(@event => @event.Description).HasMaxLength(4000).IsRequired();
        builder.HasIndex(@event => new { @event.StartsAt, @event.EndsAt });
        builder.HasIndex(@event => @event.CreatedBy);
        builder.Ignore(@event => @event.IsByGroups);

        builder.ToTable(table => table.HasCheckConstraint("CK_Events_Period", "\"EndsAt\" >= \"StartsAt\""));

        builder.HasOne(@event => @event.EventType)
            .WithMany()
            .HasForeignKey(@event => @event.EventTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EventGenerationConfiguration : IEntityTypeConfiguration<EventGeneration>
{
    public void Configure(EntityTypeBuilder<EventGeneration> builder)
    {
        builder.HasKey(item => new { item.EventId, item.GenerationId });

        builder.HasOne(item => item.Event).WithMany(@event => @event.Generations)
            .HasForeignKey(item => item.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.Generation).WithMany()
            .HasForeignKey(item => item.GenerationId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EventGroupConfiguration : IEntityTypeConfiguration<EventGroup>
{
    public void Configure(EntityTypeBuilder<EventGroup> builder)
    {
        builder.HasKey(item => new { item.EventId, item.GroupId });
        builder.HasIndex(item => item.GroupId);

        builder.HasOne(item => item.Event).WithMany(@event => @event.Groups)
            .HasForeignKey(item => item.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.Group).WithMany()
            .HasForeignKey(item => item.GroupId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EventManagerConfiguration : IEntityTypeConfiguration<EventManager>
{
    public void Configure(EntityTypeBuilder<EventManager> builder)
    {
        builder.HasKey(item => new { item.EventId, item.UserId });
        builder.HasIndex(item => item.UserId);

        builder.HasOne(item => item.Event).WithMany(@event => @event.Managers)
            .HasForeignKey(item => item.EventId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class EventAttachmentConfiguration : IEntityTypeConfiguration<EventAttachment>
{
    public void Configure(EntityTypeBuilder<EventAttachment> builder)
    {
        StoredFileConfiguration.Configure(builder);

        builder.HasOne(attachment => attachment.Event).WithMany(@event => @event.Attachments)
            .HasForeignKey(attachment => attachment.EventId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class EventCommentConfiguration : IEntityTypeConfiguration<EventComment>
{
    public void Configure(EntityTypeBuilder<EventComment> builder)
    {
        builder.Property(comment => comment.Body).HasMaxLength(4000).IsRequired();
        builder.HasIndex(comment => new { comment.EventId, comment.CreatedDate });

        builder.HasOne(comment => comment.Event).WithMany(@event => @event.Comments)
            .HasForeignKey(comment => comment.EventId).OnDelete(DeleteBehavior.Cascade);

        // Sin cascada en la autorreferencia (SQL Server no admite varias rutas de cascada); al borrar el evento
        // se borran juntos comentarios y respuestas.
        builder.HasOne(comment => comment.ParentComment).WithMany(comment => comment.Replies)
            .HasForeignKey(comment => comment.ParentCommentId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class EventCommentAttachmentConfiguration : IEntityTypeConfiguration<EventCommentAttachment>
{
    public void Configure(EntityTypeBuilder<EventCommentAttachment> builder)
    {
        StoredFileConfiguration.Configure(builder);

        builder.HasOne(attachment => attachment.Comment).WithMany(comment => comment.Attachments)
            .HasForeignKey(attachment => attachment.CommentId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal static class StoredFileConfiguration
{
    public static void Configure<T>(EntityTypeBuilder<T> builder) where T : class, IStoredFile
    {
        builder.Property(file => file.ObjectKey).HasMaxLength(300).IsRequired();
        builder.Property(file => file.FileName).HasMaxLength(255).IsRequired();
        builder.Property(file => file.ContentType).HasMaxLength(150).IsRequired();
        builder.HasIndex(file => file.ObjectKey).IsUnique();
    }
}

public class AttendeeAttendanceConfiguration : IEntityTypeConfiguration<AttendeeAttendance>
{
    public void Configure(EntityTypeBuilder<AttendeeAttendance> builder)
    {
        builder.HasKey(item => new { item.EventId, item.AttendeeId });
        builder.HasIndex(item => item.AttendeeId);

        builder.HasOne(item => item.Event).WithMany()
            .HasForeignKey(item => item.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.Attendee).WithMany()
            .HasForeignKey(item => item.AttendeeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserAttendanceConfiguration : IEntityTypeConfiguration<UserAttendance>
{
    public void Configure(EntityTypeBuilder<UserAttendance> builder)
    {
        builder.HasKey(item => new { item.EventId, item.UserId });
        builder.HasIndex(item => item.UserId);

        builder.HasOne(item => item.Event).WithMany()
            .HasForeignKey(item => item.EventId).OnDelete(DeleteBehavior.Cascade);
    }
}
