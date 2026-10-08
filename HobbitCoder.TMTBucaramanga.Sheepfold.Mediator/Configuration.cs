using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Publishers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Mediator;

public static class Configuration
{
    /// <summary>
    /// Registra el mediador y los handlers de requests y notificaciones encontrados en
    /// <paramref name="assemblies"/>. Los <see cref="IPipelineBehavior{TRequest, TResponse}"/>
    /// no se registran aquí: cada capa registra los suyos, y su orden de registro es su orden de ejecución.
    /// </summary>
    public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.TryAddScoped<IMediator, Services.Mediator>();

        services.TryAddSingleton<INotificationPublisher, ForeachAwaitPublisher>();

        var handlerContracts = new[] { typeof(IRequestHandler<,>), typeof(INotificationHandler<>) };

        var registrations = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .SelectMany(
                type => type.GetInterfaces().Where(contract =>
                    contract.IsGenericType && handlerContracts.Contains(contract.GetGenericTypeDefinition())),
                (type, contract) => (Service: contract, Implementation: type));

        foreach (var (service, implementation) in registrations)
        {
            services.AddScoped(service, implementation);
        }

        return services;
    }
}
