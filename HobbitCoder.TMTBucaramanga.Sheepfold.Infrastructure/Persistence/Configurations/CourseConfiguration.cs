using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    /// <summary>
    ///     Configura el mapeo de la entidad &lt;see cref="Course"/&gt;.
    /// </summary>
    /// <remarks>
    ///     cursos contemplados inicialmente.
    /// </remarks>
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        CatalogConfiguration.ConfigureName(builder);

        builder.HasData(
            CatalogConfiguration.Seed("0199c2a0-0000-7000-8000-000000000301", "ADN"),
            CatalogConfiguration.Seed("0199c2a0-0000-7000-8000-000000000302", "Llaves del Poder"),
            CatalogConfiguration.Seed("0199c2a0-0000-7000-8000-000000000303", "Madurez Espiritual"),
            CatalogConfiguration.Seed("0199c2a0-0000-7000-8000-000000000304", "IBLI"));
    }
}
