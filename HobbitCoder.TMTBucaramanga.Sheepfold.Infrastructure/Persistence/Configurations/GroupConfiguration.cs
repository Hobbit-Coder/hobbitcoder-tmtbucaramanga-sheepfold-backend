using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    /// <summary>
    ///     Configura el mapeo de la entidad &lt;see cref="Group"/&gt;.
    /// </summary>
    /// <remarks>
    ///     Clave candidata: año + generación + nombre.
    /// </remarks>
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.Property(group => group.Name).HasMaxLength(60).IsRequired();

        builder.HasIndex(group => new { group.Year, group.GenerationId, group.Name }).IsUnique();
        builder.HasIndex(group => group.IsActive);

        builder.HasOne(group => group.Generation)
            .WithMany()
            .HasForeignKey(group => group.GenerationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
