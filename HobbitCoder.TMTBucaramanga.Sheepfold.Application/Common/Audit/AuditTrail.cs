using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;

/// <summary>
/// Registra hechos en la auditoría ministerial. Se agrega al mismo <c>SaveChanges</c> del cambio, así que
/// ambos se guardan o fallan juntos. Los valores se guardan como una instantánea JSON del momento.
/// </summary>
public sealed class AuditTrail
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public AuditTrail(IApplicationDbContext dbContext, ICurrentUserService currentUserService, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    /// <param name="reference">Descripción legible del registro afectado tal como era (por ejemplo, "Estudio ECO / Ana Pérez").</param>
    public void Record(AuditModule module, string action, Guid? entityId, string reference, object? before, object? after)
    {
        _dbContext.AuditEntries.Add(new AuditEntry
        {
            OccurredAt = _timeProvider.GetUtcNow().UtcDateTime,
            ActorUserId = _currentUserService.UserId,
            Module = module,
            Action = action,
            EntityId = entityId,
            Reference = reference.Length > 300 ? reference[..300] : reference,
            Before = before is null ? null : JsonSerializer.Serialize(before, SerializerOptions),
            After = after is null ? null : JsonSerializer.Serialize(after, SerializerOptions)
        });
    }
}
