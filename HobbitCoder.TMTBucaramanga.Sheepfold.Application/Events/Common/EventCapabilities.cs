namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

public sealed class EventCapabilities
{
    /// <summary>Administra el evento: creador, encargado o director.</summary>
    public required bool Edit { get; set; }
    public required bool ChangeStatus { get; set; }
    public required bool Retire { get; set; }

    /// <summary>Hay registro de asistencia disponible para el usuario.</summary>
    public required bool Attendance { get; set; }
    public required bool Comment { get; set; }
}
