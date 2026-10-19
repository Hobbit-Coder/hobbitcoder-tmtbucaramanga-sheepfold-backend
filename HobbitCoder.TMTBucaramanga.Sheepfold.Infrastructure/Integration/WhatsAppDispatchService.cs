using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Integration;

/// <summary>
/// Envía las notificaciones encoladas, una a la vez, con el <see cref="IWhatsAppClient"/> configurado. Sin WhatsApp
/// configurado no se envía nada (se registra que el canal no está disponible).
/// </summary>
public sealed class WhatsAppDispatchService : BackgroundService
{
    private readonly WhatsAppNotificationQueue _queue;
    private readonly IWhatsAppClient _client;
    private readonly ILogger<WhatsAppDispatchService> _logger;

    public WhatsAppDispatchService(WhatsAppNotificationQueue queue, IWhatsAppClient client, ILogger<WhatsAppDispatchService> logger)
    {
        _queue = queue;
        _client = client;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var notification in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            if (!_client.IsConfigured)
            {
                _logger.LogWarning("WhatsApp habilitado sin credenciales del proveedor: no se envió la notificación \"{Title}\".", notification.Title);
                continue;
            }

            await _client.SendAsync(notification, stoppingToken);

            if (_client.PauseBetweenMessages > TimeSpan.Zero)
            {
                await Task.Delay(_client.PauseBetweenMessages, stoppingToken);
            }
        }
    }
}
