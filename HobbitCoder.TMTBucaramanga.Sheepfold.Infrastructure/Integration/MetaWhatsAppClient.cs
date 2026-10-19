using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Integration;

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
