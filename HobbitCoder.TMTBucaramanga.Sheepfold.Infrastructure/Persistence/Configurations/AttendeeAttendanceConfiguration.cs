using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

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
