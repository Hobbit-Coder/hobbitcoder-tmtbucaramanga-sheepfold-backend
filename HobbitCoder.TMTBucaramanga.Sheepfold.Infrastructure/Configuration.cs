using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Contexts;
using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Interceptors;
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

        return builder;
    }
}
