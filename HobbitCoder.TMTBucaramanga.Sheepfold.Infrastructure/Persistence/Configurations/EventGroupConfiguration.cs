using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

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
