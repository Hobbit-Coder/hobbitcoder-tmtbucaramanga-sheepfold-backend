namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;

public sealed class AttendeeCapabilities
{
    public required bool Edit { get; set; }
    public required bool Transfer { get; set; }
    public required bool FollowUp { get; set; }
    public required bool Retire { get; set; }
}
