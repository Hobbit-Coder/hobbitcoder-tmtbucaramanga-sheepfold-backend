using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Common;

/// <summary>Nombres de los roles en la interfaz.</summary>
public static class RoleLabels
{
    public static string? Of(UserRole? role) => role switch
    {
        UserRole.Director => "Director",
        UserRole.Coordinator => "Coordinador",
        UserRole.Leader => "Líder",
        UserRole.Apprentice => "Aprendiz",
        UserRole.Sniper => "Sniper",
        UserRole.Admin => "Administrador",
        _ => null
    };
}
