namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Reports.Common;

public sealed class ReportTable
{
    public required IReadOnlyList<string> Columns { get; set; }
    public required IReadOnlyList<ReportRow> Rows { get; set; }
}
