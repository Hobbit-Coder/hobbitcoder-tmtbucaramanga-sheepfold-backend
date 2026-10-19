namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Reports.Common;

/// <summary>Fila de la tabla equivalente al gráfico: una etiqueta y un valor por columna.</summary>
public sealed class ReportRow
{
    public required string Label { get; set; }
    public required IReadOnlyList<int> Values { get; set; }
}
