namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;

/// <summary>
/// Cuentas de Identity (fuente de verdad de los usuarios). Sheepfold no copia usuarios: los consulta aquí y
/// solo guarda sus ids en grupos, eventos, asistencia y seguimientos.
/// </summary>
public interface IUserDirectory
{
    /// <exception cref="Exceptions.ExternalServiceException">Identity no respondió.</exception>
    Task<IReadOnlyDictionary<Guid, DirectoryUser>> GetUsersAsync(CancellationToken cancellationToken);
}
