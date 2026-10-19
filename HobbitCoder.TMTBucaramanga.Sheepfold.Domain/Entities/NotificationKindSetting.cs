using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>Novedad que se notifica o no. Se envía por los canales habilitados.</summary>
public class NotificationKindSetting
{
    public required NotificationKind Kind { get; set; }
    public bool IsEnabled { get; set; }
}
