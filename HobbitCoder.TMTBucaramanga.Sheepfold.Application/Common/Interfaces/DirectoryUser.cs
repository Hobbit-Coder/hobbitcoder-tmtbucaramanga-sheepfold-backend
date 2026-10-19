using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;

public sealed class DirectoryUser
{
    public required Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public Gender? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? BirthDate { get; set; }
    public required UserRole Role { get; set; }
    public required bool IsActive { get; set; }

    /// <summary>El titular completó su primer ingreso.</summary>
    public required bool IsActivated { get; set; }
    public required bool CanRegisterAttendees { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    /// <summary>Director, coordinador, líder o aprendiz: participa de la operación ministerial.</summary>
    public bool IsMinistry => Role is not UserRole.Admin;
}
