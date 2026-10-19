using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using System.Threading.Channels;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Integration;

/// <summary>
/// Cola en memoria de notificaciones por WhatsApp. La operación que genera la notificación no espera el envío.
/// La fuente no define reintentos ni estados de entrega: un fallo se registra en el log y no se reintenta.
/// </summary>
public sealed class WhatsAppNotificationQueue : IWhatsAppNotificationSender
{
    private readonly Channel<WhatsAppNotification> _channel = Channel.CreateBounded<WhatsAppNotification>(
        new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropOldest });

    public ChannelReader<WhatsAppNotification> Reader => _channel.Reader;

    public void Enqueue(WhatsAppNotification notification) => _channel.Writer.TryWrite(notification);
}
