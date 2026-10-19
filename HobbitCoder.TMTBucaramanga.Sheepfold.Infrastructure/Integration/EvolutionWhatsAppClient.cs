using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Integration;

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
