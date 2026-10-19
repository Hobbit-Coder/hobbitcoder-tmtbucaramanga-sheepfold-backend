using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>Canal de notificación habilitado o no en la configuración.</summary>
public class NotificationChannelSetting
{
    public required NotificationChannel Channel { get; set; }
    public bool IsEnabled { get; set; }
}
