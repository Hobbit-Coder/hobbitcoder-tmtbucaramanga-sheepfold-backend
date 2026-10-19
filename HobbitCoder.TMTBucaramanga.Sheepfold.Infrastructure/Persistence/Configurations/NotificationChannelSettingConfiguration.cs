using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Configurations;

public class NotificationChannelSettingConfiguration : IEntityTypeConfiguration<NotificationChannelSetting>
{
    /// <summary>
    ///     Configura el mapeo de la entidad &lt;see cref="NotificationChannelSetting"/&gt;.
    /// </summary>
    /// <remarks>
    ///     WhatsApp queda deshabilitado hasta que se configure su pasarela.
    /// </remarks>
    public void Configure(EntityTypeBuilder<NotificationChannelSetting> builder)
    {
        builder.HasKey(setting => setting.Channel);

        builder.HasData(
            new NotificationChannelSetting { Channel = NotificationChannel.InApp, IsEnabled = true },
            new NotificationChannelSetting { Channel = NotificationChannel.WhatsApp, IsEnabled = false });
    }
}
