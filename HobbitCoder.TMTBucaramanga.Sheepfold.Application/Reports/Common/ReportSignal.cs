namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Reports.Common;

/// <summary>
/// Señal de atención. <see cref="Configured"/> falso significa que su parámetro no tiene valor ("No
/// configurado"): no se señala a nadie y se explica en la interfaz.
/// </summary>
public sealed class ReportSignal
{
    public required string Key { get; set; }
    public required bool Configured { get; set; }
    public string? Rule { get; set; }
    public required IReadOnlyList<SignalItem> Items { get; set; }
}
