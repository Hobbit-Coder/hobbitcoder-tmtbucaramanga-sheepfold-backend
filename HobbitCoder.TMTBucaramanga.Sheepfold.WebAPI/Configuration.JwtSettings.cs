using HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Common.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI;

public static partial class Configuration
{
    /// <summary>
    /// Valida los JWT que emite HobbitCoder.TMTBucaramanga.Identity. Esta API no genera tokens:
    /// solo comprueba la firma con las claves públicas del JWKS de <see cref="JwtSettings.Authority"/>,
    /// el emisor (el <c>issuer</c> de su discovery), la audiencia y la vigencia. La autorización no se aplica por defecto: cada endpoint
    /// que la requiera debe declararla con <c>[Authorize]</c> o <c>RequireAuthorization()</c>.
    /// </summary>
    /// <remarks>
    /// Solo se acepta RS256, así que un token firmado con una clave simétrica se rechaza aunque el
    /// resto de sus datos sea válido.
    /// <para>
    /// <c>MapInboundClaims</c> queda en su valor por defecto, así que los claims cortos del token se
    /// renombran a los de <c>ClaimTypes</c> (<c>sub</c> → <c>NameIdentifier</c>, <c>role</c> → <c>Role</c>).
    /// </para>
    /// </remarks>
    public static IHostApplicationBuilder AddJwtSettings(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<JwtSettings>()
            .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
            .Validate(settings => Uri.TryCreate(settings.Authority, UriKind.Absolute, out _),
                $"{JwtSettings.SectionName}:Authority es obligatorio y debe ser una URL absoluta.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Audience), $"{JwtSettings.SectionName}:Audience es obligatorio.")
            .ValidateOnStart();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        var isDevelopment = builder.Environment.IsDevelopment();

        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, jwtSettings) =>
            {
                options.Authority = jwtSettings.Value.Authority;
                options.RequireHttpsMetadata = !isDevelopment;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Sin ValidIssuer: el emisor válido es el "issuer" del discovery de Authority.
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Value.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
                };
            });

        builder.Services.AddAuthorization();

        return builder;
    }
}
