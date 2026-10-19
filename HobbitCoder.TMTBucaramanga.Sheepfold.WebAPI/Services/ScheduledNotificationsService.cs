using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Notifications.Commands;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Services;

/// <summary>
/// Genera periódicamente los avisos programados: eventos próximos, cumpleaños y pendientes. El intervalo es
/// técnico (<c>Notifications:ScheduleIntervalMinutes</c>, 60 por defecto); cuándo avisar lo definen los parámetros
/// de la configuración. Un fallo se registra y se intenta en la siguiente vuelta.
/// </summary>
public sealed class ScheduledNotificationsService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _interval;
    private readonly ILogger<ScheduledNotificationsService> _logger;

    public ScheduledNotificationsService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<ScheduledNotificationsService> logger)
    {
        _scopeFactory = scopeFactory;
        _interval = TimeSpan.FromMinutes(Math.Max(1, configuration.GetValue("Notifications:ScheduleIntervalMinutes", 60)));
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IMediator>()
                    .Send<GenerateScheduledNotificationsCommand, Unit>(new GenerateScheduledNotificationsCommand(), stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "No se pudieron generar los avisos programados.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
