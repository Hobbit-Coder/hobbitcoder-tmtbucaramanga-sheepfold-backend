using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

public class EventTypeConfiguration : IEntityTypeConfiguration<EventType>
{
    /// <summary>
    ///     Configura el mapeo de la entidad &lt;see cref="EventType"/&gt;.
    /// </summary>
    /// <remarks>
    ///     tipos iniciales; el administrador puede ajustarlos.
    /// </remarks>
    public void Configure(EntityTypeBuilder<EventType> builder)
    {
        CatalogConfiguration.ConfigureName(builder);

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
