namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;

public sealed class GroupDetailResponse
{
    public required GroupSummaryResponse Group { get; set; }
    public required IReadOnlyList<GroupMemberResponse> Members { get; set; }

    /// <summary>Se espera un líder hombre y una líder mujer. Solo informa; nunca bloquea.</summary>
    public required bool ExpectedLeadersComposition { get; set; }
    public required bool CanEdit { get; set; }
    public required bool CanManageCoordinators { get; set; }
}
