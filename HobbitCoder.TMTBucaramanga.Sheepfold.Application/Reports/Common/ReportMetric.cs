namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Reports.Common;

/// <summary>Conteo de cabecera. Las claves son estables; la interfaz las traduce.</summary>
public sealed class ReportMetric
{
    public required string Key { get; set; }
    public required int Value { get; set; }
}
