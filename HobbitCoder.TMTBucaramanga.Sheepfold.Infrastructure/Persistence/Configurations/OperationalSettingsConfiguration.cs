using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

public class OperationalSettingsConfiguration : IEntityTypeConfiguration<OperationalSettings>
{
    /// <summary>
    ///     Configura el mapeo de la entidad &lt;see cref="OperationalSettings"/&gt;.
    /// </summary>
    /// <remarks>
    ///     Una sola fila; todos los parámetros empiezan sin valor ("No configurado").
    /// </remarks>
    public void Configure(EntityTypeBuilder<OperationalSettings> builder)
    {
        builder.HasData(new { Id = OperationalSettings.SingletonId, CreatedBy = "System", CreatedDate = CatalogConfiguration.SeedDate });
    }
}
