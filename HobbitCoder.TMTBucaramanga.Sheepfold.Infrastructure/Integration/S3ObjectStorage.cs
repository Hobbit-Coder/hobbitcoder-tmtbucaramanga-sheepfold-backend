using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Integration;

/// <summary>
/// Almacenamiento S3 compatible (Contabo Object Storage, RT-02). Usa solo PutObject, GetObject y DeleteObject sobre
/// un bucket privado: los objetos no tienen ACL pública ni se generan URLs; la API entrega el contenido después de
/// autorizar. Las sumas de verificación se calculan solo cuando la operación las exige, porque no todos los
/// servicios compatibles admiten las que el SDK agrega por defecto.
/// </summary>
public sealed class S3ObjectStorage : IObjectStorage, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly StorageSettings _settings;
    private readonly ILogger<S3ObjectStorage> _logger;

    public S3ObjectStorage(IOptions<StorageSettings> options, ILogger<S3ObjectStorage> logger)
    {
        _settings = options.Value;
        _logger = logger;

        var config = new AmazonS3Config
        {
            ServiceURL = _settings.ServiceUrl,
            ForcePathStyle = _settings.ForcePathStyle,
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
        };

        if (!string.IsNullOrWhiteSpace(_settings.Region))
        {
            config.AuthenticationRegion = _settings.Region;
        }

        _client = new AmazonS3Client(new BasicAWSCredentials(_settings.AccessKey, _settings.SecretKey), config);
    }

    public long MaxUploadBytes => _settings.MaxUploadBytes;

    public async Task PutAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken)
    {
        try
        {
            await _client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _settings.Bucket,
                Key = objectKey,
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = false
            }, cancellationToken);
        }
        catch (AmazonServiceException exception)
        {
            _logger.LogError(exception, "El almacenamiento rechazó la carga de {ObjectKey}.", objectKey);
            throw new ExternalServiceException("The file could not be stored.", exception);
        }
    }

    public async Task<Stream> GetAsync(string objectKey, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.GetObjectAsync(_settings.Bucket, objectKey, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonServiceException exception)
        {
            _logger.LogError(exception, "No se pudo leer {ObjectKey} del almacenamiento.", objectKey);
            throw new ExternalServiceException("The file could not be read.", exception);
        }
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        try
        {
            await _client.DeleteObjectAsync(_settings.Bucket, objectKey, cancellationToken);
        }
        catch (AmazonServiceException exception)
        {
            throw new ExternalServiceException("The file could not be deleted.", exception);
        }
    }

    public void Dispose() => _client.Dispose();
}

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
