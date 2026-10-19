namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Context.Common;

public sealed class MyCapabilities
{
    /// <summary>Accede a calendario, personas, grupos y eventos.</summary>
    public required bool Ministry { get; set; }
    public required bool CreateEvents { get; set; }
    public required bool FollowUps { get; set; }
    public required bool RegisterAttendees { get; set; }
    public required bool Reports { get; set; }
    public required bool Audit { get; set; }
    public required bool ManageConfiguration { get; set; }
    public required bool ViewConfiguration { get; set; }
    public required bool RetiredRecords { get; set; }
    public required bool ManageGroups { get; set; }
}
