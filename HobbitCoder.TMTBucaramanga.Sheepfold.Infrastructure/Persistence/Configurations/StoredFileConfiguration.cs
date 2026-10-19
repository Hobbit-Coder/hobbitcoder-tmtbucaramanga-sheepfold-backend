using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

internal static class StoredFileConfiguration
{
    public static void Configure<T>(EntityTypeBuilder<T> builder) where T : class, IStoredFile
    {
        builder.Property(file => file.ObjectKey).HasMaxLength(300).IsRequired();
        builder.Property(file => file.FileName).HasMaxLength(255).IsRequired();
        builder.Property(file => file.ContentType).HasMaxLength(150).IsRequired();
        builder.HasIndex(file => file.ObjectKey).IsUnique();
    }
}
