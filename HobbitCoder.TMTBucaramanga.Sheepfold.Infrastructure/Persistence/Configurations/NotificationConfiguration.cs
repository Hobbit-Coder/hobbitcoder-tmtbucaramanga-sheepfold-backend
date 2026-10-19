using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

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
