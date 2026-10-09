using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

/// <summary>
/// Envía las notificaciones encoladas a la pasarela: <c>POST {GatewayUrl}</c> con
/// <c>{ channel: "whatsapp", to, template: "notification", parameters: { title, message } }</c>. Sin pasarela
/// configurada no se envía nada (se registra que el canal no está disponible).
/// </summary>
public sealed class WhatsAppDispatchService : BackgroundService
{
    public const string HttpClientName = "messaging-gateway";

    private readonly WhatsAppNotificationQueue _queue;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MessagingSettings _settings;
    private readonly ILogger<WhatsAppDispatchService> _logger;

    public WhatsAppDispatchService(WhatsAppNotificationQueue queue, IHttpClientFactory httpClientFactory, IOptions<MessagingSettings> options, ILogger<WhatsAppDispatchService> logger)
    {
        _queue = queue;
        _httpClientFactory = httpClientFactory;
        _settings = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var notification in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            if (string.IsNullOrWhiteSpace(_settings.GatewayUrl))
            {
                _logger.LogWarning("WhatsApp habilitado sin pasarela configurada: no se envió la notificación \"{Title}\".", notification.Title);
                continue;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, _settings.GatewayUrl)
                {
                    Content = JsonContent.Create(new
                    {
                        channel = "whatsapp",
                        to = notification.PhoneNumber,
                        template = "notification",
                        parameters = new { title = notification.Title, message = notification.Message }
                    })
                };

                if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
                }

                using var response = await _httpClientFactory.CreateClient(HttpClientName).SendAsync(request, stoppingToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("La pasarela de mensajería respondió {StatusCode} a una notificación de WhatsApp.", (int)response.StatusCode);
                }
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(exception, "No se pudo enviar una notificación de WhatsApp.");
            }
        }
    }
}
