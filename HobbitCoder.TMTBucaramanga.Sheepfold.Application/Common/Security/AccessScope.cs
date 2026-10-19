using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;

/// <summary>Responsabilidades del usuario en grupos, de todos los años (para la consulta histórica).</summary>
public sealed class AccessScope
{
    public AccessScope(Guid userId, UserRole role, IReadOnlyList<ScopeGroup> groups)
    {
        UserId = userId;
        Role = role;
        Groups = groups;

        CoordinatedGroupIds = Ids(GroupResponsibility.Coordinator);
        LedGroupIds = Ids(GroupResponsibility.Leader);
        ApprenticeGroupIds = Ids(GroupResponsibility.Apprentice);
        ManagedGroupIds = [.. CoordinatedGroupIds, .. LedGroupIds];
        OperationalGroupIds = [.. ManagedGroupIds, .. ApprenticeGroupIds];
        ActiveManagedGroupIds = groups.Where(group => group.IsActive && ManagedGroupIds.Contains(group.GroupId)).Select(group => group.GroupId).ToHashSet();
        ActiveOperationalGroupIds = groups.Where(group => group.IsActive).Select(group => group.GroupId).ToHashSet();
        ActiveGenerationIds = groups.Where(group => group.IsActive).Select(group => group.GenerationId).ToHashSet();
    }

    public Guid UserId { get; }
    public UserRole Role { get; }
    public IReadOnlyList<ScopeGroup> Groups { get; }
    public bool IsDirector => Role == UserRole.Director;

    public HashSet<Guid> CoordinatedGroupIds { get; }
    public HashSet<Guid> LedGroupIds { get; }
    public HashSet<Guid> ApprenticeGroupIds { get; }

    /// <summary>Grupos que coordina o lidera: seguimientos y administración de grupos.</summary>
    public HashSet<Guid> ManagedGroupIds { get; }

    /// <summary>Grupos donde además es aprendiz: consulta y edición de asistentes y asistencia.</summary>
    public HashSet<Guid> OperationalGroupIds { get; }

    public HashSet<Guid> ActiveManagedGroupIds { get; }
    public HashSet<Guid> ActiveOperationalGroupIds { get; }
    public HashSet<Guid> ActiveGenerationIds { get; }

    /// <summary>Grupos activos para los que puede crear eventos: los que coordina o lidera.</summary>
    public HashSet<Guid> EventGroupIds => Role is UserRole.Coordinator or UserRole.Leader ? ActiveManagedGroupIds : [];

    private HashSet<Guid> Ids(GroupResponsibility responsibility) =>
        Groups.Where(group => group.Responsibility == responsibility).Select(group => group.GroupId).ToHashSet();
}
