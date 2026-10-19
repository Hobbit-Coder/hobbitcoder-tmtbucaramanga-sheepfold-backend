namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Calendar.Common;

/// <summary>
/// Seguimiento pendiente con fecha programada. No lleva nombre, tema ni lugar: el detalle autorizado se abre aparte
///. Sin hora no se inventa una hora ni una duración.
/// </summary>
public sealed class CalendarFollowUpItem
{
    public required Guid Id { get; set; }
    public required DateOnly Date { get; set; }
    public TimeOnly? Time { get; set; }
    public required bool IsConfidential { get; set; }
}
