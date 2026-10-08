using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Testcontainers.PostgreSql;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.IntegrationTests.Infrastructure;

/// <summary>
/// Sheepfold contra un PostgreSQL en Docker con las migraciones aplicadas. En lugar de descargar el
/// discovery de Identity, la validación JWT recibe un discovery fijo con el issuer y la clave pública de
/// prueba: el resto de la configuración (audiencia, RS256, vigencia) es la de producción.
/// </summary>
/// <remarks>
/// Que el discovery real de Identity sea compatible con esta configuración lo prueban las pruebas de
/// integración de HobbitCoder.TMTBucaramanga.Identity.
/// </remarks>
public sealed class SheepfoldApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Issuer = "https://identity.tmtbucaramanga.test";
    public const string Audience = "HobbitCoder.TMTBucaramanga.Sheepfold";

    private static readonly JsonWebTokenHandler TokenHandler = new();

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private readonly RsaSecurityKey _signingKey = new(RSA.Create(2048)) { KeyId = "clave-de-prueba" };

    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync();

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<SheepfoldContext>().Database.MigrateAsync();
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    /// <summary>Token como los que emite Identity, firmado con la clave de prueba.</summary>
    public string CreateToken(
        string subject = "usuario-de-prueba",
        string issuer = Issuer,
        string audience = Audience,
        SigningCredentials? signingCredentials = null)
    {
        var now = DateTime.UtcNow;

        return TokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(10),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = subject,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString()
            },
            SigningCredentials = signingCredentials ?? new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256)
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Persistence:Provider"] = "PostgreSQL",
            ["ConnectionStrings:Default"] = _database.GetConnectionString(),
            ["JwtSettings:Authority"] = Issuer
        }));

        builder.ConfigureTestServices(services =>
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var discovery = new OpenIdConnectConfiguration { Issuer = Issuer };
                discovery.SigningKeys.Add(_signingKey);

                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(discovery);
            }));
    }
}
