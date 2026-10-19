using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Integration;

/// <summary>Sin credenciales de almacenamiento: las operaciones con archivos responden 503; no hay disco local de respaldo.</summary>
public sealed class UnavailableObjectStorage : IObjectStorage
{
    private const string Message = "File storage is not configured.";

    public UnavailableObjectStorage(IOptions<StorageSettings> options)
    {
        MaxUploadBytes = options.Value.MaxUploadBytes;
    }

    public long MaxUploadBytes { get; }

    public Task PutAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken) =>
        throw new ExternalServiceException(Message);

    public Task<Stream> GetAsync(string objectKey, CancellationToken cancellationToken) =>
        throw new ExternalServiceException(Message);

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) =>
        throw new ExternalServiceException(Message);
}
