using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Common;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Calendar.Common;

/// <summary>Cumpleaños: fecha sin hora. Solo nombre y fecha; nada de datos de contacto.</summary>
public sealed class BirthdayItem
{
    public required PersonType PersonType { get; set; }
    public required Guid PersonId { get; set; }
    public required string FullName { get; set; }
    public required DateOnly Date { get; set; }
    public int? TurnsAge { get; set; }
}
