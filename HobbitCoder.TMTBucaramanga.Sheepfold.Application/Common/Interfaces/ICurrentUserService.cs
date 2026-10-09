using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;

/// <summary>Usuario autenticado según el token de Identity.</summary>
public interface ICurrentUserService
{
    /// <summary>Claim <c>sub</c>, o <c>System</c> fuera de una petición (tareas programadas).</summary>
    string UserId { get; }

    /// <summary>Rol del claim <c>role</c>; <c>null</c> si el token no trae un rol conocido.</summary>
    UserRole? Role { get; }

    /// <summary>Indica si el token trae el permiso (claim <c>permission</c>).</summary>
    bool HasPermission(string permission);
}
