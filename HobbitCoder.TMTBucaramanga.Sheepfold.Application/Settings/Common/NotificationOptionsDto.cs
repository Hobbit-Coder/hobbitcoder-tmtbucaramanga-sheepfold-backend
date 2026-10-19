using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Common;

public sealed class NotificationOptionsDto
{
    public required IReadOnlyDictionary<NotificationChannel, bool> Channels { get; set; }
    public required IReadOnlyDictionary<NotificationKind, bool> Kinds { get; set; }
}
