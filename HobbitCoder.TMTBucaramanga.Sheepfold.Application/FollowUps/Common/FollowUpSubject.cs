using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Common;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Common;

public sealed class FollowUpSubject
{
    public required PersonType Type { get; set; }
    public required Guid Id { get; set; }
    public required string FullName { get; set; }

    /// <summary>Grupo actual del asistente o rol del usuario.</summary>
    public string? Context { get; set; }
}
