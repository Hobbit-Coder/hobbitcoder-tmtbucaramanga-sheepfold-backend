using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Audit.Common;

public sealed class AuditEntryResponse
{
    public required Guid Id { get; set; }
    public required DateTime OccurredAt { get; set; }
    public required string ActorUserId { get; set; }
    public required string ActorName { get; set; }
    public required AuditModule Module { get; set; }
    public required string Action { get; set; }
    public Guid? EntityId { get; set; }
    public required string Reference { get; set; }
    public string? Before { get; set; }
    public string? After { get; set; }
}
