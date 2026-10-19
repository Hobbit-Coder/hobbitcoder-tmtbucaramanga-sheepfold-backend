namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;

/// <summary>
/// Almacenamiento de objetos privado (S3 alojado en Contabo) para los archivos que cargan los usuarios.
/// La base solo guarda metadatos; la autorización la decide el handler antes de llamar aquí.
/// </summary>
public interface IObjectStorage
{
    /// <summary>Tamaño máximo de un archivo, configurable (<c>Storage:MaxUploadBytes</c>).</summary>
    long MaxUploadBytes { get; }

    /// <exception cref="Exceptions.ExternalServiceException">El almacenamiento no está configurado o falló.</exception>
    Task PutAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken);

    /// <exception cref="Exceptions.ExternalServiceException">El almacenamiento no está configurado o falló.</exception>
    Task<Stream> GetAsync(string objectKey, CancellationToken cancellationToken);

    /// <summary>Borra el objeto; no falla si ya no existe.</summary>
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}
