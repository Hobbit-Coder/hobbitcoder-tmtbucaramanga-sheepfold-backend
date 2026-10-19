using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

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
