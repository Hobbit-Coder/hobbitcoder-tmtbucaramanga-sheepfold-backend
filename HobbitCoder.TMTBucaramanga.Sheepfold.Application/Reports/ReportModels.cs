namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Reports;

/// <summary>Familias de reportes (§24, S09).</summary>
public enum ReportFamily
{
    Attendance = 1,
    People = 2,
    FollowUps = 3,
    Courses = 4,
    Events = 5,
    Birthdays = 6
}

/// <summary>Conteo de cabecera. Las claves son estables; la interfaz las traduce.</summary>
public sealed record ReportMetric(string Key, int Value);

/// <summary>Fila de la tabla equivalente al gráfico: una etiqueta y un valor por columna.</summary>
public sealed record ReportRow(string Label, IReadOnlyList<int> Values);

public sealed record ReportTable(IReadOnlyList<string> Columns, IReadOnlyList<ReportRow> Rows);

public sealed record SignalItem(Guid? Id, string Name, string Detail);

/// <summary>
/// Señal de atención (§24). <see cref="Configured"/> falso significa que su parámetro no tiene valor ("No
/// configurado", decisión P04): no se señala a nadie y se explica en la interfaz.
/// </summary>
public sealed record ReportSignal(string Key, bool Configured, string? Rule, IReadOnlyList<SignalItem> Items);

public sealed record ReportResponse(
    ReportFamily Family,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<ReportMetric> Metrics,
    ReportTable Table,
    IReadOnlyList<ReportSignal> Signals);
