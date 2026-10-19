namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>Metadatos de un archivo guardado en el almacenamiento de objetos.</summary>
public interface IStoredFile
{
    string ObjectKey { get; }
    string FileName { get; }
    string ContentType { get; }
    long SizeBytes { get; }
}
