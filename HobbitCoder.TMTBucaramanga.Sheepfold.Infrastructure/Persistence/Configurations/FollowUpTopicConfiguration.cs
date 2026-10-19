using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

/// <summary>La fuente no enumera temas de seguimiento: los registra el administrador.</summary>
public class FollowUpTopicConfiguration : IEntityTypeConfiguration<FollowUpTopic>
{
    public void Configure(EntityTypeBuilder<FollowUpTopic> builder)
    {
        CatalogConfiguration.ConfigureName(builder);
    }
}
