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

/// <summary>Proveedor que entrega una notificación por WhatsApp (Evolution o Meta, según <see cref="MessagingSettings.Provider"/>).</summary>
public interface IWhatsAppClient
{
    bool IsConfigured { get; }

    /// <summary>Pausa que el despachador espera después de cada envío.</summary>
    TimeSpan PauseBetweenMessages { get; }

    /// <summary>Envía la notificación; un rechazo o un fallo de red se registra y no se propaga.</summary>
    /// <returns><c>true</c> si el proveedor aceptó el mensaje.</returns>
    Task<bool> SendAsync(WhatsAppNotification notification, CancellationToken cancellationToken);
}

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

/// <summary>
/// Cliente de Evolution API (v2): confirma con <c>POST {Url}/chat/whatsappNumbers/{Instance}</c> que el teléfono tiene
/// WhatsApp y envía el aviso como texto con <c>POST {Url}/message/sendText/{Instance}</c>, ambos con la cabecera
/// <c>apikey</c>.
/// </summary>
public sealed class EvolutionWhatsAppClient : IWhatsAppClient
{
    public const string HttpClientName = "whatsapp";

    /// <summary>Milisegundos que Evolution muestra "escribiendo…" antes de enviar.</summary>
    public const int TypingDelayMilliseconds = 1000;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EvolutionSettings _settings;
    private readonly ILogger<EvolutionWhatsAppClient> _logger;

    public EvolutionWhatsAppClient(IHttpClientFactory httpClientFactory, IOptions<MessagingSettings> options, ILogger<EvolutionWhatsAppClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = options.Value.Evolution;
        _logger = logger;
    }

    public bool IsConfigured => _settings.IsConfigured;

    public TimeSpan PauseBetweenMessages => TimeSpan.FromSeconds(_settings.SecondsBetweenMessages);

    public async Task<bool> SendAsync(WhatsAppNotification notification, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var baseUrl = _settings.Url!.TrimEnd('/');
        var instance = Uri.EscapeDataString(_settings.Instance!);
        var digits = new string(notification.PhoneNumber.Where(char.IsDigit).ToArray());

        try
        {
            using var check = NewRequest($"{baseUrl}/chat/whatsappNumbers/{instance}", new { numbers = new[] { digits } });
            using var checkResponse = await client.SendAsync(check, cancellationToken);

            if (!checkResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Evolution API respondió {StatusCode} al verificar el teléfono de una notificación: {Error}",
                    (int)checkResponse.StatusCode, await WhatsAppError.ReadAsync(checkResponse, cancellationToken));
                return false;
            }

            var number = await NumberAsync(checkResponse, cancellationToken);

            if (number is null)
            {
                _logger.LogWarning("El destinatario de la notificación \"{Title}\" no tiene WhatsApp.", notification.Title);
                return false;
            }

            using var send = NewRequest($"{baseUrl}/message/sendText/{instance}", new { number, text = Text(notification), delay = TypingDelayMilliseconds });
            using var sendResponse = await client.SendAsync(send, cancellationToken);

            if (sendResponse.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogError("Evolution API respondió {StatusCode} a una notificación: {Error}",
                (int)sendResponse.StatusCode, await WhatsAppError.ReadAsync(sendResponse, cancellationToken));
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException
            || exception is TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "No se pudo enviar una notificación de WhatsApp.");
        }

        return false;
    }

    /// <summary>Texto del aviso: título en negrilla, mensaje y firma.</summary>
    public static string Text(WhatsAppNotification notification) =>
        $"*{notification.Title.Trim()}*\n{notification.Message.Trim()}\n\n_TMT Bucaramanga_";

    private HttpRequestMessage NewRequest(string url, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return request;
    }

    /// <summary>Número con el que WhatsApp conoce al teléfono (la parte del <c>jid</c> antes de <c>@</c>), o <c>null</c>.</summary>
    private static async Task<string?> NumberAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0 || root[0].ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var result = root[0];

        return result.TryGetProperty("exists", out var exists) && exists.ValueKind == JsonValueKind.True
            && result.TryGetProperty("jid", out var jid) && jid.ValueKind == JsonValueKind.String
                ? jid.GetString()!.Split('@')[0]
                : null;
    }
}

/// <summary>
/// Cliente de WhatsApp Cloud API: <c>POST {BaseUrl}/{ApiVersion}/{PhoneNumberId}/messages</c> con la plantilla de
/// utilidad <see cref="MetaWhatsAppSettings.NotificationTemplate"/> y dos variables del cuerpo (título y mensaje). Meta
/// no admite saltos de línea, tabulaciones ni más de cuatro espacios seguidos en las variables, así que se compactan.
/// </summary>
public sealed partial class MetaWhatsAppClient : IWhatsAppClient
{
    private const int MaxTitleLength = 60;
    private const int MaxMessageLength = 500;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MetaWhatsAppSettings _settings;
    private readonly ILogger<MetaWhatsAppClient> _logger;

    public MetaWhatsAppClient(IHttpClientFactory httpClientFactory, IOptions<MessagingSettings> options, ILogger<MetaWhatsAppClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = options.Value.Meta;
        _logger = logger;
    }

    public bool IsConfigured => _settings.IsConfigured;

    public TimeSpan PauseBetweenMessages => TimeSpan.Zero;

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
            using var response = await _httpClientFactory.CreateClient(EvolutionWhatsAppClient.HttpClientName).SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogError("WhatsApp Cloud API respondió {StatusCode} a una notificación: {Error}",
                (int)response.StatusCode, await WhatsAppError.ReadAsync(response, cancellationToken));
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

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}

/// <summary>
/// Resume el error del proveedor para el log sin el cuerpo completo: <c>error.code</c> y <c>error.message</c> (Meta) o
/// <c>response.message</c> (Evolution API).
/// </summary>
public static class WhatsAppError
{
    private const string NoDetail = "sin detalle";

    public static async Task<string> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return NoDetail;
            }

            if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object)
            {
                return $"{Property(error, "code")} {Property(error, "message")}";
            }

            if (root.TryGetProperty("response", out var detail) && detail.ValueKind == JsonValueKind.Object
                && detail.TryGetProperty("message", out var message))
            {
                return message.ValueKind == JsonValueKind.Array
                    ? string.Join("; ", message.EnumerateArray().Select(item => item.ToString()))
                    : message.ToString();
            }

            return NoDetail;
        }
        catch (JsonException)
        {
            return NoDetail;
        }
    }

    private static string Property(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) ? value.ToString() : string.Empty;
}
