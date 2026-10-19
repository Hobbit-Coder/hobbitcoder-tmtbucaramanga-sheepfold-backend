namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

/// <summary>Rol de la cuenta en Identity (claim <c>role</c>). Sheepfold no guarda roles: los lee del token o del directorio.</summary>
public enum UserRole
{
    Admin = 1,
    Director = 2,
    Coordinator = 3,
    Leader = 4,
    Apprentice = 5
}
