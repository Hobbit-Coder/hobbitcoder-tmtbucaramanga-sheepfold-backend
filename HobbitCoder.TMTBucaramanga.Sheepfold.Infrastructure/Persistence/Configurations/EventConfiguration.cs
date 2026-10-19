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
