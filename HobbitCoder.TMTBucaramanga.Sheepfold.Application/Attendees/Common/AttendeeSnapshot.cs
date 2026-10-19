using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;

public sealed class AttendeeSnapshot
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public Gender? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Instagram { get; set; }
    public DateOnly? BirthDate { get; set; }
    public EducationalStage? EducationalStage { get; set; }
    public required bool IsServer { get; set; }
    public required bool IsActive { get; set; }
}
