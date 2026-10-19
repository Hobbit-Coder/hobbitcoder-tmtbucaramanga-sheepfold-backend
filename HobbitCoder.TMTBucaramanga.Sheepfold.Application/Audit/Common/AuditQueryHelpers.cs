using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Audit.Common;

public static class AuditQueryHelpers
{
    public static AuditEntryResponse ToResponse(AuditEntry entry, IReadOnlyDictionary<Guid, DirectoryUser> directory) =>
        new AuditEntryResponse
        {
            Id = entry.Id,
            OccurredAt = entry.OccurredAt,
            ActorUserId = entry.ActorUserId,
            ActorName = directory.NameOf(entry.ActorUserId),
            Module = entry.Module,
            Action = entry.Action,
            EntityId = entry.EntityId,
            Reference = entry.Reference,
            Before = entry.Before,
            After = entry.After
        };
}
