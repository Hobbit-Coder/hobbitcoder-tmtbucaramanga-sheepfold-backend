using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using ValidationException = HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Common;

public static class FollowUpCommandHelpers
{
    public static object Snapshot(FollowUp followUp) => new
    {
        followUp.Status,
        followUp.ScheduledDate,
        followUp.ScheduledTime,
        followUp.MeetingDate,
        followUp.Location,
        followUp.TopicId,
        followUp.Description,
        followUp.Commitments,
        followUp.Observations,
        followUp.IsConfidential
    };

    /// <summary>
    /// Grupo del asistente el día <paramref name="date"/> (hora de Bogotá; si cambió ese día, el último). Si la
    /// reunión es anterior a su primera asignación, el grupo con el que se registró.
    /// </summary>
    public static async Task<Guid?> GroupOnAsync(IApplicationDbContext dbContext, Guid attendeeId, DateOnly date, CancellationToken cancellationToken)
    {
        var endOfDay = LocalCalendar.StartOfDayUtc(date.AddDays(1)).AddTicks(-1);
        var memberships = await dbContext.GroupMemberships
            .Where(membership => membership.AttendeeId == attendeeId)
            .OrderBy(membership => membership.StartedAt)
            .ToListAsync(cancellationToken);

        return (memberships.LastOrDefault(membership => membership.WasActiveAt(endOfDay)) ?? memberships.FirstOrDefault())?.GroupId;
    }

    public static async Task<FollowUp> FindEditableAsync(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory, Guid id, CancellationToken cancellationToken)
    {
        if (!access.CanFollowUp)
        {
            throw new ForbiddenAccessException();
        }

        var directory = await userDirectory.GetUsersAsync(cancellationToken);
        var followUp = await (await access.VisibleFollowUpsAsync(dbContext.FollowUps, directory, cancellationToken))
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(FollowUp), id);

        return access.CanEditFollowUp(followUp) ? followUp : throw new ForbiddenAccessException();
    }

    public static async Task ApplyContentAsync(IApplicationDbContext dbContext, FollowUp followUp, FollowUpContent content, CancellationToken cancellationToken)
    {
        var topicIsValid = await dbContext.FollowUpTopics.AnyAsync(topic => topic.Id == content.TopicId && (topic.IsActive || topic.Id == followUp.TopicId), cancellationToken);

        if (!topicIsValid)
        {
            throw ValidationException.For("TopicId", "Select a topic from the list.");
        }

        followUp.Status = FollowUpStatus.Done;
        followUp.MeetingDate = content.MeetingDate;
        followUp.Location = content.Location.Trim();
        followUp.TopicId = content.TopicId;
        followUp.Description = content.Description.Trim();
        followUp.Commitments = content.Commitments.Trim();
        followUp.Observations = string.IsNullOrWhiteSpace(content.Observations) ? null : content.Observations.Trim();
        followUp.IsConfidential = content.IsConfidential;

        if (followUp.SubjectAttendeeId is { } attendeeId)
        {
            followUp.GroupId = await GroupOnAsync(dbContext, attendeeId, content.MeetingDate, cancellationToken);
        }
    }
}
