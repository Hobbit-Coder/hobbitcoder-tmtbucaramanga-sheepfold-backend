using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;

/// <summary>Datos personales editables de la ficha. Solo nombres y apellidos son obligatorios.</summary>
public interface IAttendeeData
{
    string FirstName { get; }
    string LastName { get; }
    Gender? Gender { get; }
    string? PhoneNumber { get; }
    string? Instagram { get; }
    DateOnly? BirthDate { get; }
    EducationalStage? EducationalStage { get; }
    bool IsServer { get; }
}
