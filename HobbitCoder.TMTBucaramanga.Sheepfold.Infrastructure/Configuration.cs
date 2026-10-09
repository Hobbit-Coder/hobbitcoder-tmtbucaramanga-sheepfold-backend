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

        builder.Services.AddOptions<MessagingSettings>().Bind(configuration.GetSection(MessagingSettings.SectionName));
        builder.Services.AddHttpClient(WhatsAppDispatchService.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(10));
        builder.Services.AddSingleton<WhatsAppNotificationQueue>();
        builder.Services.AddSingleton<IWhatsAppNotificationSender>(serviceProvider => serviceProvider.GetRequiredService<WhatsAppNotificationQueue>());
        builder.Services.AddHostedService<WhatsAppDispatchService>();
    }
}
