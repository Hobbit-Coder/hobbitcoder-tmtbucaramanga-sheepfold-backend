using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

/// <summary>
/// Listas administrables. El nombre es clave candidata dentro de cada lista, también frente a valores
/// retirados (inactivos, se pueden restaurar). Incluye valores iniciales.
/// </summary>
internal static class CatalogConfiguration
{
    public const int NameMaxLength = 100;

    /// <summary>Fecha fija de los datos semilla (las migraciones no deben cambiar en cada generación).</summary>
    public static readonly DateTime SeedDate = new(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc);

    public static void ConfigureName<T>(EntityTypeBuilder<T> builder) where T : CatalogItem
    {
        builder.Property(item => item.Name).HasMaxLength(NameMaxLength).IsRequired();
        builder.HasIndex(item => item.Name).IsUnique();
    }

    public static object Seed(string id, string name) =>
        new { Id = Guid.Parse(id), Name = name, CreatedBy = "System", CreatedDate = SeedDate, IsActive = true };
}
