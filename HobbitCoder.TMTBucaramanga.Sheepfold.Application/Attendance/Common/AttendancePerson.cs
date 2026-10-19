using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Common;

public sealed class AttendancePerson
{
    public required PersonType PersonType { get; set; }
    public required Guid PersonId { get; set; }
    public required string FullName { get; set; }
    public UserRole? Role { get; set; }
    public required bool IsServer { get; set; }

    /// <summary>Grupo de ese momento (para filtrar), si tiene.</summary>
    public Guid? GroupId { get; set; }

    /// <summary>Grupo de la persona en la fecha del evento, o su rol si no pertenece a un grupo.</summary>
    public string? Context { get; set; }
    public AttendanceMark? Mark { get; set; }
}
