using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

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
