using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Seeding;

/// <summary>
/// Al arrancar, inserta <see cref="SheepfoldSeed"/> si <c>Seeding:Enabled</c> está activo y la carga no se ha
/// hecho. La carga va en un solo <c>SaveChanges</c> (una transacción): o queda completa o no queda nada. La
/// generación PAPÁS, que solo crea esta carga, marca que ya se hizo; así no reaparece lo que se borre después.
/// </summary>
/// <remarks>
/// Si falla (base sin migrar o inaccesible) la aplicación no arranca: se pidió la carga explícitamente.
/// Identity debe sembrarse primero para que las responsabilidades apunten a cuentas existentes.
/// </remarks>
public class SheepfoldSeeder : IHostedService
{
    public const string EnabledKey = "Seeding:Enabled";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SheepfoldSeeder> _logger;

    public SheepfoldSeeder(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<SheepfoldSeeder> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.GetValue<bool>(EnabledKey))
        {
            return;
        }

        var seed = SheepfoldSeed.Load();
        var markerId = seed.Generations[0].Id;

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SheepfoldContext>();

        if (await dbContext.Generations.AnyAsync(generation => generation.Id == markerId, cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var @event in seed.Events)
        {
            @event.Status = @event.EndsAt < now ? EventStatus.Finished : EventStatus.Published;
        }

        dbContext.Generations.AddRange(seed.Generations);
        dbContext.Courses.AddRange(seed.Courses);
        dbContext.EventTypes.AddRange(seed.EventTypes);
        dbContext.Groups.AddRange(seed.Groups);
        dbContext.GroupAssignments.AddRange(seed.Assignments);
        dbContext.Attendees.AddRange(seed.Attendees);
        dbContext.Events.AddRange(seed.Events);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeding: se cargaron {Groups} grupos, {Attendees} asistentes y {Events} eventos.",
            seed.Groups.Count, seed.Attendees.Count, seed.Events.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
