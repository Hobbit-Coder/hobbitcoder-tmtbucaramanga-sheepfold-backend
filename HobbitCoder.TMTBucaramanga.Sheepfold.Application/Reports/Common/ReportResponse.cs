namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Reports.Common;

public sealed class ReportResponse
{
    public required ReportFamily Family { get; set; }
    public required DateOnly From { get; set; }
    public required DateOnly To { get; set; }
    public required IReadOnlyList<ReportMetric> Metrics { get; set; }
    public required ReportTable Table { get; set; }
    public required IReadOnlyList<ReportSignal> Signals { get; set; }
}
