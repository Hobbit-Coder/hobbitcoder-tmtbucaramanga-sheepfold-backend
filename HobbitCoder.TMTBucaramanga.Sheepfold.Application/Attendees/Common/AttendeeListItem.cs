namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;

public sealed class AttendeeListItem
{
    public required Guid Id { get; set; }
    public required string FullName { get; set; }
    public GroupRef? Group { get; set; }
    public required bool IsServer { get; set; }
    public required bool IsActive { get; set; }
}
