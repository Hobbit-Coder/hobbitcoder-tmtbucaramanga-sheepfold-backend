using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

public class NotificationKindSettingConfiguration : IEntityTypeConfiguration<NotificationKindSetting>
{
    public void Configure(EntityTypeBuilder<NotificationKindSetting> builder)
    {
        builder.HasKey(setting => setting.Kind);
        builder.HasData(Enum.GetValues<NotificationKind>().Select(kind => new NotificationKindSetting { Kind = kind, IsEnabled = true }));
    }
}
