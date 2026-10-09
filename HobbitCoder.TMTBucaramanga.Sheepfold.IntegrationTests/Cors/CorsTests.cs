using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.IntegrationTests.Cors;

/// <summary>
/// CORS con la configuración real de cada ambiente (<c>appsettings.{Ambiente}.json</c>). Una petición
/// preflight no llega a los controllers, así que no hace falta base de datos.
/// </summary>
/// <remarks>
/// En producción el frontend vive en el mismo dominio que esta API (<c>https://sheepfold.tmtbucaramanga.com</c>):
/// sus llamadas son del mismo origen y no pasan por CORS, así que no se permite ningún otro origen.
/// </remarks>
public class CorsTests
{
    private const string DevelopmentFrontend = "http://localhost:4200";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Development_AllowsTheLocalFrontendWithToken()
    {
        using var factory = new CorsFactory(Environments.Development);
        using var client = factory.CreateClient();

        using var response = await SendPreflightAsync(client, DevelopmentFrontend, "PATCH");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(DevelopmentFrontend, GetHeader(response, "Access-Control-Allow-Origin"));
        Assert.Contains("PATCH", GetHeader(response, "Access-Control-Allow-Methods"));
        Assert.Contains("authorization", GetHeader(response, "Access-Control-Allow-Headers"), StringComparison.OrdinalIgnoreCase);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Theory]
    [InlineData(DevelopmentFrontend)]
    [InlineData("https://otro-sitio.example.com")]
    public async Task Production_DoesNotAllowCrossOriginCalls(string origin)
    {
        using var factory = new CorsFactory(Environments.Production);
        using var client = factory.CreateClient();

        using var response = await SendPreflightAsync(client, origin, "GET");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    /// <summary>
    /// El frontend de Identity lee las responsabilidades de una cuenta en esta API (editor de usuarios); el frontend
    /// de Sheepfold comparte origen y no necesita CORS.
    /// </summary>
    [Fact]
    public async Task Production_AllowsOnlyTheIdentityFrontend()
    {
        using var factory = new CorsFactory(Environments.Production);
        using var client = factory.CreateClient();

        using var response = await SendPreflightAsync(client, "https://identity.tmtbucaramanga.com", "GET");

        Assert.Equal("https://identity.tmtbucaramanga.com", GetHeader(response, "Access-Control-Allow-Origin"));
    }

    [Theory]
    [InlineData("*", "no admite '*'")]
    [InlineData("https://sheepfold.tmtbucaramanga.com/", "sin barra final")]
    [InlineData("sheepfold.tmtbucaramanga.com", "debe ser una URL absoluta")]
    [InlineData("http://sheepfold.tmtbucaramanga.com", "debe usar https fuera de desarrollo")]
    public void InvalidOrigin_PreventsStartup(string origin, string expectedError)
    {
        using var factory = new CorsFactory(Environments.Production, origin);

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient().Dispose());

        var validationException = FindValidationException(exception);
        Assert.NotNull(validationException);
        Assert.Contains(expectedError, validationException.Message);
    }

    private static OptionsValidationException? FindValidationException(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is OptionsValidationException validationException)
            {
                return validationException;
            }
        }

        return null;
    }

    private static async Task<HttpResponseMessage> SendPreflightAsync(HttpClient client, string origin, string method)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/me/context");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", method);
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        return await client.SendAsync(request, CancellationToken);
    }

    private static string GetHeader(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : string.Empty;

    private sealed class CorsFactory(string environmentName, string? extraOrigin = null) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environmentName);

            if (extraOrigin is not null)
            {
                builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CorsSettings:AllowedOrigins:0"] = extraOrigin
                }));
            }
        }
    }
}
