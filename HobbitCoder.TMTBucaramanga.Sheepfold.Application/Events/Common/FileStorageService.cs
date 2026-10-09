using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using ValidationException = HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

/// <summary>
/// Carga de archivos al almacenamiento S3 privado (RT-02) con confirmación y compensación: primero se suben los
/// objetos, luego se guardan sus metadatos; si la base falla, se borran los objetos ya subidos para no dejar
/// huérfanos. Nunca se guarda el contenido en la base ni se emiten URLs públicas.
/// </summary>
public sealed class FileStorageService
{
    private const int MaxFileNameLength = 255;
    private const string DefaultContentType = "application/octet-stream";

    private readonly IObjectStorage _storage;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IObjectStorage storage, ILogger<FileStorageService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    /// <summary>Sube los archivos con claves nuevas bajo <paramref name="prefix"/>.</summary>
    /// <returns>Archivos subidos con su clave de objeto.</returns>
    public async Task<IReadOnlyList<StoredUpload>> UploadAsync(string prefix, IReadOnlyCollection<UploadedFile> files, CancellationToken cancellationToken)
    {
        var tooLarge = files.FirstOrDefault(file => file.SizeBytes > _storage.MaxUploadBytes);

        if (tooLarge is not null)
        {
            throw ValidationException.For("Files", $"'{tooLarge.FileName}' exceeds the maximum size of {_storage.MaxUploadBytes / (1024 * 1024)} MB.");
        }

        var uploaded = new List<StoredUpload>();

        try
        {
            foreach (var file in files)
            {
                var key = $"{prefix}/{Guid.CreateVersion7():N}";
                await using var content = file.OpenReadStream();
                var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? DefaultContentType : file.ContentType;

                await _storage.PutAsync(key, content, contentType, cancellationToken);
                uploaded.Add(new StoredUpload(key, SafeFileName(file.FileName), contentType, file.SizeBytes));
            }
        }
        catch
        {
            await CompensateAsync(uploaded.Select(upload => upload.ObjectKey));
            throw;
        }

        return uploaded;
    }

    /// <summary>Borra objetos cuyo registro no llegó a guardarse; un fallo aquí solo se registra.</summary>
    public async Task CompensateAsync(IEnumerable<string> objectKeys)
    {
        foreach (var key in objectKeys)
        {
            try
            {
                await _storage.DeleteAsync(key, CancellationToken.None);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "No se pudo borrar el objeto huérfano {ObjectKey}; requiere limpieza manual.", key);
            }
        }
    }

    public Task<Stream> OpenAsync(string objectKey, CancellationToken cancellationToken) => _storage.GetAsync(objectKey, cancellationToken);

    private static string SafeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName.Replace('\\', '/'));
        name = string.IsNullOrWhiteSpace(name) ? "archivo" : name.Trim();

        return name.Length > MaxFileNameLength ? name[^MaxFileNameLength..] : name;
    }
}

public sealed record StoredUpload(string ObjectKey, string FileName, string ContentType, long SizeBytes);
