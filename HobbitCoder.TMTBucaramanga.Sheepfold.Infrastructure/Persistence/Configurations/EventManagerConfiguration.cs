using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

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
