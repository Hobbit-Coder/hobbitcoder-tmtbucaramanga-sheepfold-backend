using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;

public sealed class ContactResponse
{
    public required Guid Id { get; set; }
    public required ContactKind Kind { get; set; }
    public required string FullName { get; set; }
    public required string Relationship { get; set; }
    public required string PhoneNumber { get; set; }
}
