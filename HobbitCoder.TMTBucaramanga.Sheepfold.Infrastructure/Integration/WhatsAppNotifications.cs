using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
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
/// Envía las notificaciones encoladas con <see cref="WhatsAppCloudClient"/>. Sin WhatsApp configurado no se envía
/// nada (se registra que el canal no está disponible).
/// </summary>
public sealed class WhatsAppDispatchService : BackgroundService
{
    private readonly WhatsAppNotificationQueue _queue;
    private readonly WhatsAppCloudClient _client;
    private readonly ILogger<WhatsAppDispatchService> _logger;

    public WhatsAppDispatchService(WhatsAppNotificationQueue queue, WhatsAppCloudClient client, ILogger<WhatsAppDispatchService> logger)
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
                _logger.LogWarning("WhatsApp habilitado sin credenciales de WhatsApp Cloud API: no se envió la notificación \"{Title}\".", notification.Title);
                continue;
            }

            await _client.SendAsync(notification, stoppingToken);
        }
    }
}

/// <summary>
/// Cliente de WhatsApp Cloud API: <c>POST {BaseUrl}/{ApiVersion}/{PhoneNumberId}/messages</c> con la plantilla de
/// utilidad <see cref="MessagingSettings.NotificationTemplate"/> y dos variables del cuerpo (título y mensaje). Meta no
/// admite saltos de línea, tabulaciones ni más de cuatro espacios seguidos en las variables, así que se compactan.
/// </summary>
public sealed partial class WhatsAppCloudClient
{
    public const string HttpClientName = "whatsapp-cloud";
    private const int MaxTitleLength = 60;
    private const int MaxMessageLength = 500;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MessagingSettings _settings;
    private readonly ILogger<WhatsAppCloudClient> _logger;

    public WhatsAppCloudClient(IHttpClientFactory httpClientFactory, IOptions<MessagingSettings> options, ILogger<WhatsAppCloudClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => _settings.IsConfigured;

    /// <summary>Envía la notificación; un rechazo o un fallo de red se registra y no se propaga.</summary>
    /// <returns><c>true</c> si Meta aceptó el mensaje.</returns>
    public async Task<bool> SendAsync(WhatsAppNotification notification, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl.TrimEnd('/')}/{_settings.ApiVersion}/{_settings.PhoneNumberId}/messages")
        {
            Content = JsonContent.Create(new
            {
                messaging_product = "whatsapp",
                to = new string(notification.PhoneNumber.Where(char.IsDigit).ToArray()),
                type = "template",
                template = new
                {
                    name = _settings.NotificationTemplate,
                    language = new { code = _settings.TemplateLanguage },
                    components = new[]
                    {
                        new
                        {
                            type = "body",
                            parameters = new[]
                            {
                                new { type = "text", text = Parameter(notification.Title, MaxTitleLength) },
                                new { type = "text", text = Parameter(notification.Message, MaxMessageLength) }
                            }
                        }
                    }
                }
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AccessToken);

        try
        {
            using var response = await _httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogError("WhatsApp Cloud API respondió {StatusCode} a una notificación: {Error}",
                (int)response.StatusCode, await ErrorSummaryAsync(response, cancellationToken));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "No se pudo enviar una notificación de WhatsApp.");
        }

        return false;
    }

    /// <summary>Texto apto para una variable de plantilla: sin saltos de línea ni espacios repetidos y con longitud acotada.</summary>
    public static string Parameter(string value, int maxLength)
    {
        var compact = Whitespace().Replace(value, " ").Trim();

        return compact.Length > maxLength ? $"{compact[..(maxLength - 1)].TrimEnd()}…" : compact;
    }

    /// <summary>Resume el error de la Graph API (<c>error.code</c> y <c>error.message</c>) para el log, sin el cuerpo completo.</summary>
    public static async Task<string> ErrorSummaryAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = document.RootElement;

            return root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object
                ? $"{Property(error, "code")} {Property(error, "message")}"
                : "sin detalle";
        }
        catch (JsonException)
        {
            return "sin detalle";
        }
    }

    private static string Property(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) ? value.ToString() : string.Empty;

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
