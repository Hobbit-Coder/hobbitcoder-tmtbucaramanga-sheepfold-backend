using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

public class AttendeeConfiguration : IEntityTypeConfiguration<Attendee>
{
    public void Configure(EntityTypeBuilder<Attendee> builder)
    {
        builder.Property(attendee => attendee.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(attendee => attendee.LastName).HasMaxLength(100).IsRequired();
        builder.Property(attendee => attendee.PhoneNumber).HasMaxLength(30);
        builder.Property(attendee => attendee.Instagram).HasMaxLength(60);
        builder.HasIndex(attendee => new { attendee.LastName, attendee.FirstName });
        builder.Ignore(attendee => attendee.FullName);
    }
}
