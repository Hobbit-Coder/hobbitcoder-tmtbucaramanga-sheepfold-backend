using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

public class AttendeeContactConfiguration : IEntityTypeConfiguration<AttendeeContact>
{
    public void Configure(EntityTypeBuilder<AttendeeContact> builder)
    {
        builder.Property(contact => contact.FullName).HasMaxLength(150).IsRequired();
        builder.Property(contact => contact.Relationship).HasMaxLength(60).IsRequired();
        builder.Property(contact => contact.PhoneNumber).HasMaxLength(30).IsRequired();

        builder.HasOne(contact => contact.Attendee)
            .WithMany(attendee => attendee.Contacts)
            .HasForeignKey(contact => contact.AttendeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
