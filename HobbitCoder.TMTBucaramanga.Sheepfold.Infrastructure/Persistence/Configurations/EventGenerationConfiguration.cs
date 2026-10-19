using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

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
