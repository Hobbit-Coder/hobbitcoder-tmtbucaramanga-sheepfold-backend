namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

/// <summary>Archivo recibido en una petición, antes de guardarlo en el almacenamiento.</summary>
public sealed class UploadedFile
{
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long SizeBytes { get; set; }
    public required Func<Stream> OpenReadStream { get; set; }
}
