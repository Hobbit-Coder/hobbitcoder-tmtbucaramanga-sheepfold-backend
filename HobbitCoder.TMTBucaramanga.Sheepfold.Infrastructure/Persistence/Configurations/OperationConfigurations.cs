using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

public class FollowUpConfiguration : IEntityTypeConfiguration<FollowUp>
{
    public void Configure(EntityTypeBuilder<FollowUp> builder)
    {
        builder.Property(followUp => followUp.Location).HasMaxLength(150);
        builder.Property(followUp => followUp.Description).HasMaxLength(4000);
        builder.Property(followUp => followUp.Commitments).HasMaxLength(4000);
        builder.Property(followUp => followUp.Observations).HasMaxLength(4000);
        builder.HasIndex(followUp => followUp.CreatedBy);
        builder.HasIndex(followUp => followUp.SubjectUserId);
        builder.Ignore(followUp => followUp.AuthorUserId);

        builder.ToTable(table =>
        {
            // Exactamente una persona atendida: un asistente o un usuario.
            table.HasCheckConstraint("CK_FollowUps_Subject",
                "(\"SubjectAttendeeId\" IS NULL AND \"SubjectUserId\" IS NOT NULL) OR (\"SubjectAttendeeId\" IS NOT NULL AND \"SubjectUserId\" IS NULL)");

            // La hora programada solo existe con fecha programada.
            table.HasCheckConstraint("CK_FollowUps_ScheduledTime", "\"ScheduledTime\" IS NULL OR \"ScheduledDate\" IS NOT NULL");

            // Un realizado tiene fecha, lugar, tema, descripción y compromisos (§21).
            table.HasCheckConstraint("CK_FollowUps_Done",
                "\"Status\" <> 'Done' OR (\"MeetingDate\" IS NOT NULL AND \"Location\" IS NOT NULL AND \"TopicId\" IS NOT NULL AND \"Description\" IS NOT NULL AND \"Commitments\" IS NOT NULL)");
        });

        builder.HasOne(followUp => followUp.SubjectAttendee).WithMany()
            .HasForeignKey(followUp => followUp.SubjectAttendeeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(followUp => followUp.Topic).WithMany()
            .HasForeignKey(followUp => followUp.TopicId).OnDelete(DeleteBehavior.Restrict);
        // Un grupo con historial de asistentes no se elimina definitivamente, así que la referencia no queda huérfana.
        builder.HasOne(followUp => followUp.Group).WithMany()
            .HasForeignKey(followUp => followUp.GroupId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(notification => notification.Title).HasMaxLength(200).IsRequired();
        builder.Property(notification => notification.Message).HasMaxLength(500).IsRequired();
        builder.HasIndex(notification => new { notification.RecipientUserId, notification.CreatedAt });

        builder.HasOne(notification => notification.Event).WithMany()
            .HasForeignKey(notification => notification.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(notification => notification.Attendee).WithMany()
            .HasForeignKey(notification => notification.AttendeeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class NotificationChannelSettingConfiguration : IEntityTypeConfiguration<NotificationChannelSetting>
{
    public void Configure(EntityTypeBuilder<NotificationChannelSetting> builder)
    {
        builder.HasKey(setting => setting.Channel);

        // WhatsApp queda deshabilitado hasta que se configure su pasarela.
        builder.HasData(
            new NotificationChannelSetting { Channel = NotificationChannel.InApp, IsEnabled = true },
            new NotificationChannelSetting { Channel = NotificationChannel.WhatsApp, IsEnabled = false });
    }
}

public class NotificationKindSettingConfiguration : IEntityTypeConfiguration<NotificationKindSetting>
{
    public void Configure(EntityTypeBuilder<NotificationKindSetting> builder)
    {
        builder.HasKey(setting => setting.Kind);
        builder.HasData(Enum.GetValues<NotificationKind>().Select(kind => new NotificationKindSetting { Kind = kind, IsEnabled = true }));
    }
}

public class OperationalSettingsConfiguration : IEntityTypeConfiguration<OperationalSettings>
{
    public void Configure(EntityTypeBuilder<OperationalSettings> builder)
    {
        // Una sola fila; todos los parámetros empiezan sin valor ("No configurado", decisión P04).
        builder.HasData(new { Id = OperationalSettings.SingletonId, CreatedBy = "System", CreatedDate = CatalogConfiguration.SeedDate });
    }
}

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.Property(entry => entry.ActorUserId).HasMaxLength(256).IsRequired();
        builder.Property(entry => entry.Action).HasMaxLength(60).IsRequired();
        builder.Property(entry => entry.Reference).HasMaxLength(300).IsRequired();
        builder.HasIndex(entry => entry.OccurredAt);
        builder.HasIndex(entry => new { entry.Module, entry.OccurredAt });
        builder.HasIndex(entry => entry.EntityId);
    }
}
