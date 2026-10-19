using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>
/// Archivo adjunto al evento. El contenido vive en el almacenamiento S3 privado; aquí solo los metadatos.
/// </summary>
public class EventAttachment : SoftDeleteableEntity, IStoredFile
{
    public required Guid EventId { get; set; }
    public required string ObjectKey { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long SizeBytes { get; set; }

    public virtual Event Event { get; set; } = null!;
}
