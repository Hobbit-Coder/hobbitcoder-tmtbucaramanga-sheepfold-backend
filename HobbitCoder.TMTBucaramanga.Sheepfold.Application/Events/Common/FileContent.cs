namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

/// <summary>Contenido de un archivo para descargar.</summary>
public sealed class FileContent
{
    public required Stream Content { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
}
