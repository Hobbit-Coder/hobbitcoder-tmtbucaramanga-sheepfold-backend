using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

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
