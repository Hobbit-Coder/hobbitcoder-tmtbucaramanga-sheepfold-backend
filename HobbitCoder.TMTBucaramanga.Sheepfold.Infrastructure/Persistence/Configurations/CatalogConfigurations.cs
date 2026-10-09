using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

/// <summary>
/// Listas administrables (§27). El nombre es clave candidata dentro de cada lista, también frente a valores
/// retirados (inactivos, se pueden restaurar). Los valores iniciales son los que enumera el requerimiento.
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

public class GenerationConfiguration : IEntityTypeConfiguration<Generation>
{
    public void Configure(EntityTypeBuilder<Generation> builder)
    {
        CatalogConfiguration.ConfigureName(builder);
        builder.Ignore(generation => generation.RequiredContactKind);

        builder.HasData(
            Seed("0199c2a0-0000-7000-8000-000000000101", "PULSO", GenerationProfile.Pulso, 1),
            Seed("0199c2a0-0000-7000-8000-000000000102", "ECO", GenerationProfile.Eco, 2),
            Seed("0199c2a0-0000-7000-8000-000000000103", "LEGADO 1-5", GenerationProfile.Legado1To5, 3),
            Seed("0199c2a0-0000-7000-8000-000000000104", "LEGADO 6-10", GenerationProfile.Legado6To10, 4));
    }

    private static object Seed(string id, string name, GenerationProfile profile, int sortOrder) =>
        new
        {
            Id = Guid.Parse(id),
            Name = name,
            Profile = profile,
            SortOrder = sortOrder,
            CreatedBy = "System",
            CreatedDate = CatalogConfiguration.SeedDate,
            IsActive = true
        };
}

public class EventTypeConfiguration : IEntityTypeConfiguration<EventType>
{
    public void Configure(EntityTypeBuilder<EventType> builder)
    {
        CatalogConfiguration.ConfigureName(builder);

        // §15: tipos que el requerimiento enumera; el administrador puede ajustarlos.
        builder.HasData(
            CatalogConfiguration.Seed("0199c2a0-0000-7000-8000-000000000201", "Reunión general de los sábados"),
            CatalogConfiguration.Seed("0199c2a0-0000-7000-8000-000000000202", "Estudio TMT"),
            CatalogConfiguration.Seed("0199c2a0-0000-7000-8000-000000000203", "Reunión de planeación"),
            CatalogConfiguration.Seed("0199c2a0-0000-7000-8000-000000000204", "Entrenamiento de servidores"),
            CatalogConfiguration.Seed("0199c2a0-0000-7000-8000-000000000205", "Reunión de coordinadores"),
            CatalogConfiguration.Seed("0199c2a0-0000-7000-8000-000000000206", "Reunión de servidores"),
            CatalogConfiguration.Seed("0199c2a0-0000-7000-8000-000000000207", "Campamento"),
            CatalogConfiguration.Seed("0199c2a0-0000-7000-8000-000000000208", "Salida"),
            CatalogConfiguration.Seed("0199c2a0-0000-7000-8000-000000000209", "Actividad con venta de boletas"));
    }
}

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        CatalogConfiguration.ConfigureName(builder);

        // §10: cursos contemplados inicialmente.
        builder.HasData(
            CatalogConfiguration.Seed("0199c2a0-0000-7000-8000-000000000301", "ADN"),
            CatalogConfiguration.Seed("0199c2a0-0000-7000-8000-000000000302", "Llaves del Poder"),
            CatalogConfiguration.Seed("0199c2a0-0000-7000-8000-000000000303", "Madurez Espiritual"),
            CatalogConfiguration.Seed("0199c2a0-0000-7000-8000-000000000304", "IBLI"));
    }
}

/// <summary>La fuente no enumera temas de seguimiento: los registra el administrador.</summary>
public class FollowUpTopicConfiguration : IEntityTypeConfiguration<FollowUpTopic>
{
    public void Configure(EntityTypeBuilder<FollowUpTopic> builder)
    {
        CatalogConfiguration.ConfigureName(builder);
    }
}
