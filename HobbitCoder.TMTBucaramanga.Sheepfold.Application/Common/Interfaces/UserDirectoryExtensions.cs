namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;

public static class UserDirectoryExtensions
{
    /// <summary>Nombre para mostrar, o "Usuario eliminado" si la cuenta ya no existe en Identity.</summary>
    public static string NameOf(this IReadOnlyDictionary<Guid, DirectoryUser> users, Guid userId) =>
        users.TryGetValue(userId, out var user) ? user.FullName : "Usuario eliminado";

    public static string NameOf(this IReadOnlyDictionary<Guid, DirectoryUser> users, string userId) =>
        Guid.TryParse(userId, out var id) ? users.NameOf(id) : "Sistema";
}
