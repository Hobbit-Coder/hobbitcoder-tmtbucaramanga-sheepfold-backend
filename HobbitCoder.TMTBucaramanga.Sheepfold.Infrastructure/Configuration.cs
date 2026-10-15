using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Integration;
using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Contexts;
using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Interceptors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure;

public static class Configuration
{
    /// <summary>
    /// Registra los servicios de infraestructura comunes a todos los proveedores de base de datos.
    /// </summary>
    /// <remarks>
    /// <see cref="IApplicationDbContext"/> se resuelve desde la instancia de <see cref="SheepfoldContext"/>
    /// registrada por <c>AddDbContext</c>, para que en un mismo scope ambos compartan el ChangeTracker.
    /// </remarks>
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IApplicationDbContext>(serviceProvider => serviceProvider.GetRequiredService<SheepfoldContext>());
        builder.Services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        builder.AddIntegrations();

        return builder;
    }

    /// <summary>
    /// Integraciones externas: directorio de Identity, almacenamiento S3 (Contabo) y pasarela de WhatsApp. Sin
    /// almacenamiento configurado las operaciones con archivos responden 503: no se usa el disco del contenedor.
    /// </summary>
    private static void AddIntegrations(this IHostApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        builder.Services.AddMemoryCache();

        builder.Services.AddOptions<IdentityDirectorySettings>()
            .Bind(configuration.GetSection(IdentityDirectorySettings.SectionName))
            .Validate(settings => string.IsNullOrWhiteSpace(settings.BaseUrl) || Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out _),
                $"{IdentityDirectorySettings.SectionName}:BaseUrl debe ser una URL absoluta.")
            .ValidateOnStart();
        builder.Services.AddHttpClient(IdentityUserDirectory.HttpClientName, (serviceProvider, client) =>
        {
            var baseUrl = configuration[$"{IdentityDirectorySettings.SectionName}:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                // La barra final hace que las rutas relativas ("api/...") se resuelvan bajo la URL base.
                client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/");
            }

            client.Timeout = TimeSpan.FromSeconds(10);
        });
        builder.Services.AddSingleton<IUserDirectory, IdentityUserDirectory>();

        var storage = configuration.GetSection(StorageSettings.SectionName);
        builder.Services.AddOptions<StorageSettings>()
            .Bind(storage)
            .Validate(settings => !settings.IsConfigured || settings.ServiceUrl.StartsWith("https://", StringComparison.Ordinal) || builder.Environment.IsDevelopment(),
                $"{StorageSettings.SectionName}:ServiceUrl debe usar https.")
            .Validate(settings => settings.MaxUploadBytes > 0, $"{StorageSettings.SectionName}:MaxUploadBytes debe ser mayor que cero.")
            .ValidateOnStart();

        if ((storage.Get<StorageSettings>() ?? new StorageSettings()).IsConfigured)
        {
            builder.Services.AddSingleton<IObjectStorage, S3ObjectStorage>();
        }
        else
        {
            builder.Services.AddSingleton<IObjectStorage, UnavailableObjectStorage>();
        }

        builder.Services.AddOptions<MessagingSettings>()
            .Bind(configuration.GetSection(MessagingSettings.SectionName))
            .Validate(settings => Enum.IsDefined(settings.Provider), $"{MessagingSettings.SectionName}:Provider debe ser Evolution o Meta.")
            .Validate(settings => string.IsNullOrWhiteSpace(settings.Evolution.Url) || IsAbsoluteUrl(settings.Evolution.Url, Uri.UriSchemeHttp, Uri.UriSchemeHttps),
                $"{MessagingSettings.SectionName}:Evolution:Url debe ser una URL absoluta http o https.")
            .Validate(settings => settings.Evolution.SecondsBetweenMessages >= 0, $"{MessagingSettings.SectionName}:Evolution:SecondsBetweenMessages no puede ser negativo.")
            .Validate(settings => IsAbsoluteUrl(settings.Meta.BaseUrl, Uri.UriSchemeHttps), $"{MessagingSettings.SectionName}:Meta:BaseUrl debe ser una URL absoluta https.")
            .Validate(settings => settings.TimeoutSeconds > 0, $"{MessagingSettings.SectionName}:TimeoutSeconds debe ser mayor que cero.")
            .ValidateOnStart();
        var messaging = configuration.GetSection(MessagingSettings.SectionName).Get<MessagingSettings>() ?? new MessagingSettings();
        builder.Services.AddHttpClient(EvolutionWhatsAppClient.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(messaging.TimeoutSeconds));

        if (messaging.Provider == MessagingProvider.Meta)
        {
            builder.Services.AddSingleton<IWhatsAppClient, MetaWhatsAppClient>();
        }
        else
        {
            builder.Services.AddSingleton<IWhatsAppClient, EvolutionWhatsAppClient>();
        }

        builder.Services.AddSingleton<WhatsAppNotificationQueue>();
        builder.Services.AddSingleton<IWhatsAppNotificationSender>(serviceProvider => serviceProvider.GetRequiredService<WhatsAppNotificationQueue>());
        builder.Services.AddHostedService<WhatsAppDispatchService>();
    }

    private static bool IsAbsoluteUrl(string value, params string[] schemes) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && schemes.Contains(uri.Scheme);
}
