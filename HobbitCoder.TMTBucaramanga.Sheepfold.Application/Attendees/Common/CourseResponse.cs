using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;

public sealed class CourseResponse
{
    public required Guid Id { get; set; }
    public required Guid CourseId { get; set; }
    public required string CourseName { get; set; }
    public required CourseStatus Status { get; set; }
    public DateOnly? FinishedOn { get; set; }
}
