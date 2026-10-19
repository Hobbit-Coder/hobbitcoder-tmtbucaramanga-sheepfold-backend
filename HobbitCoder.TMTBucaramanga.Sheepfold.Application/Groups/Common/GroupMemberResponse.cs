namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;

public sealed class GroupMemberResponse
{
    public required Guid AttendeeId { get; set; }
    public required string FullName { get; set; }
    public required bool IsServer { get; set; }
    public required bool IsActive { get; set; }
    public required DateTime Since { get; set; }
}
