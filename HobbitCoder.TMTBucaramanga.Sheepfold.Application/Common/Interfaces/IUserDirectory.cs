using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

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

/// <param name="IsActivated">El titular completó su primer ingreso.</param>
public sealed record DirectoryUser(
    Guid Id,
    string FirstName,
    string LastName,
    Gender? Gender,
    string? PhoneNumber,
    DateOnly? BirthDate,
    UserRole Role,
    bool IsActive,
    bool IsActivated,
    bool CanRegisterAttendees)
{
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>Director, coordinador, líder o aprendiz: participa de la operación ministerial.</summary>
    public bool IsMinistry => Role is not UserRole.Admin;
}

public static class UserDirectoryExtensions
{
    /// <summary>Nombre para mostrar, o "Usuario eliminado" si la cuenta ya no existe en Identity.</summary>
    public static string NameOf(this IReadOnlyDictionary<Guid, DirectoryUser> users, Guid userId) =>
        users.TryGetValue(userId, out var user) ? user.FullName : "Usuario eliminado";

    public static string NameOf(this IReadOnlyDictionary<Guid, DirectoryUser> users, string userId) =>
        Guid.TryParse(userId, out var id) ? users.NameOf(id) : "Sistema";
}
