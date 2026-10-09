using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;

/// <summary>
/// Registra hechos en la auditoría ministerial (§25). Se agrega al mismo <c>SaveChanges</c> del cambio, así que
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

/// <summary>Acciones de la auditoría ministerial. Son códigos estables; la interfaz los traduce.</summary>
public static class AuditActions
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Retired = "Retired";
    public const string Restored = "Restored";
    public const string PermanentlyDeleted = "PermanentlyDeleted";
    public const string Activated = "Activated";
    public const string Deactivated = "Deactivated";
    public const string Transferred = "Transferred";
    public const string TeamChanged = "TeamChanged";
    public const string StatusChanged = "StatusChanged";
    public const string AttendanceMarked = "AttendanceMarked";
    public const string AttendanceCleared = "AttendanceCleared";
    public const string Completed = "Completed";
    public const string ConfidentialityChanged = "ConfidentialityChanged";
    public const string SensitiveRead = "SensitiveRead";
    public const string ContactAdded = "ContactAdded";
    public const string ContactUpdated = "ContactUpdated";
    public const string ContactRemoved = "ContactRemoved";
    public const string CourseAdded = "CourseAdded";
    public const string CourseUpdated = "CourseUpdated";
    public const string CourseRemoved = "CourseRemoved";
    public const string AttachmentAdded = "AttachmentAdded";
    public const string AttachmentRemoved = "AttachmentRemoved";

    /// <summary>Acción de un cambio que puede activar o desactivar el registro.</summary>
    public static string ForActivation(bool wasActive, bool isActive, string activated = Activated, string deactivated = Deactivated)
    {
        if (wasActive == isActive)
        {
            return Updated;
        }

        return isActive ? activated : deactivated;
    }
}
