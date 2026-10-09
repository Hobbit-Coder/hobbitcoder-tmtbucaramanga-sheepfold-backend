using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using System.Collections.Concurrent;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.IntegrationTests.Infrastructure;

/// <summary>Doble de prueba del directorio de Identity: las pruebas registran aquí sus cuentas.</summary>
public sealed class TestUserDirectory : IUserDirectory
{
    private readonly ConcurrentDictionary<Guid, DirectoryUser> _users = new();

    public void Add(DirectoryUser user) => _users[user.Id] = user;

    public Task<IReadOnlyDictionary<Guid, DirectoryUser>> GetUsersAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, DirectoryUser>>(new Dictionary<Guid, DirectoryUser>(_users));
}

/// <summary>
/// Doble de prueba del almacenamiento S3: guarda los objetos en memoria y puede simular un fallo en la siguiente
/// carga. Solo existe en el proyecto de pruebas; la validación real contra Contabo es aparte.
/// </summary>
public sealed class InMemoryObjectStorage : IObjectStorage
{
    private readonly ConcurrentDictionary<string, (byte[] Content, string ContentType)> _objects = new();

    public long MaxUploadBytes { get; set; } = 1_000_000;

    public bool FailNextPut { get; set; }

    public IReadOnlyCollection<string> Keys => _objects.Keys.ToList();

    public async Task PutAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken)
    {
        if (FailNextPut)
        {
            FailNextPut = false;
            throw new ExternalServiceException("Simulated storage failure.");
        }

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        _objects[objectKey] = (buffer.ToArray(), contentType);
    }

    public Task<Stream> GetAsync(string objectKey, CancellationToken cancellationToken) =>
        _objects.TryGetValue(objectKey, out var stored)
            ? Task.FromResult<Stream>(new MemoryStream(stored.Content))
            : throw new ExternalServiceException("Missing object.");

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        _objects.TryRemove(objectKey, out _);
        return Task.CompletedTask;
    }
}
