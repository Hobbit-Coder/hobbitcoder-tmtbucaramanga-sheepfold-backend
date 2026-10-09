using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Integration;

/// <summary>
/// Directorio de cuentas leído de Identity (<c>GET /api/directory/users</c>) con un token de servicio
/// (<c>POST /api/auth/service-token</c>). Se guarda en memoria unos segundos: es una caché, no una copia
/// persistente de los usuarios.
/// </summary>
public class IdentityUserDirectory : IUserDirectory
{
    public const string HttpClientName = "identity-directory";
    private const string DirectoryCacheKey = "identity-directory:users";
    private const string TokenCacheKey = "identity-directory:token";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IdentityDirectorySettings _settings;
    private readonly ILogger<IdentityUserDirectory> _logger;

    public IdentityUserDirectory(IHttpClientFactory httpClientFactory, IMemoryCache cache, IOptions<IdentityDirectorySettings> options, ILogger<IdentityUserDirectory> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<Guid, DirectoryUser>> GetUsersAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(DirectoryCacheKey, out IReadOnlyDictionary<Guid, DirectoryUser>? cached) && cached is not null)
        {
            return cached;
        }

        if (string.IsNullOrWhiteSpace(_settings.BaseUrl) || string.IsNullOrWhiteSpace(_settings.ClientSecret))
        {
            throw new ExternalServiceException("The Identity directory is not configured.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/directory/users");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(client, cancellationToken));

            using var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var users = await response.Content.ReadFromJsonAsync<List<DirectoryUser>>(JsonOptions, cancellationToken) ?? [];
            var directory = users.ToDictionary(user => user.Id);

            _cache.Set(DirectoryCacheKey, (IReadOnlyDictionary<Guid, DirectoryUser>)directory, TimeSpan.FromSeconds(_settings.CacheSeconds));
            return directory;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "No se pudo consultar el directorio de Identity.");
            _cache.Remove(TokenCacheKey);
            throw new ExternalServiceException("The Identity directory is not available.", exception);
        }
    }

    private async Task<string> GetTokenAsync(HttpClient client, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(TokenCacheKey, out string? token) && token is not null)
        {
            return token;
        }

        using var response = await client.PostAsJsonAsync("api/auth/service-token",
            new { clientId = _settings.ClientId, clientSecret = _settings.ClientSecret }, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ServiceToken>(JsonOptions, cancellationToken)
            ?? throw new JsonException("Empty service token response.");

        // Se renueva un minuto antes de vencer.
        _cache.Set(TokenCacheKey, body.AccessToken, TimeSpan.FromSeconds(Math.Max(30, body.ExpiresIn - 60)));
        return body.AccessToken;
    }

    private sealed record ServiceToken(string AccessToken, int ExpiresIn);
}
