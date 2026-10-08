using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Services;

/// <summary>
/// Mediador que resuelve el handler de un request y lo ejecuta envuelto en la cadena
/// de <see cref="IPipelineBehavior{TRequest, TResponse}"/> registrada, y que publica
/// notificaciones delegando la ejecución de sus handlers en el <see cref="INotificationPublisher"/>.
/// </summary>
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationPublisher _notificationPublisher;

    public Mediator(IServiceProvider serviceProvider, INotificationPublisher notificationPublisher)
    {
        _serviceProvider = serviceProvider;
        _notificationPublisher = notificationPublisher;
    }

    /// <summary>
    /// Envía un request al handler correspondiente.
    /// Los behaviors se aplican en el orden en que fueron registrados: el primero
    /// registrado envuelve a los demás.
    /// </summary>
    public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

        RequestHandlerDelegate<TResponse> pipeline = token => handler.Handle(request, token);

        var behaviors = _serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToArray();

        for (var index = behaviors.Length - 1; index >= 0; index--)
        {
            var behavior = behaviors[index];
            var next = pipeline;

            pipeline = token => behavior.Handle(request, next, token);
        }

        return pipeline(cancellationToken);
    }

    /// <summary>
    /// Publica una notificación a todos sus handlers.
    /// Los handlers se resuelven por el tipo estático <typeparamref name="TNotification"/>, igual
    /// que los requests en <see cref="Send{TRequest, TResponse}"/>. Las notificaciones no pasan
    /// por los <see cref="IPipelineBehavior{TRequest, TResponse}"/>.
    /// </summary>
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        var handlerExecutors = _serviceProvider.GetServices<INotificationHandler<TNotification>>()
            .Select(handler => new NotificationHandlerExecutor(
                handler,
                (notificationToHandle, token) => handler.Handle((TNotification)notificationToHandle, token)));

        return _notificationPublisher.Publish(handlerExecutors, notification, cancellationToken);
    }
}
