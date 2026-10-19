namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Reports.Common;

public sealed class SignalItem
{
    public Guid? Id { get; set; }
    public required string Name { get; set; }
    public required string Detail { get; set; }
}
