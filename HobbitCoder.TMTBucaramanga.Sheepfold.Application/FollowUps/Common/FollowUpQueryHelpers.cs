using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Common;

public static class FollowUpQueryHelpers
{
    /// <summary>El aprendiz y el administrador no tienen acceso pastoral.</summary>
    public static void EnsurePastoralAccess(MinistryAccess access)
    {
        if (!access.CanFollowUp)
        {
            throw new ForbiddenAccessException();
        }
    }

    /// <summary>
    ///     Convierte los seguimientos en elementos de lista con su grupo y responsable.
    /// </summary>
    /// <remarks>
    ///     Los realizados muestran el grupo de la reunión, no el actual.
    /// </remarks>
    public static async Task<List<FollowUpListItem>> ToListItemsAsync(IApplicationDbContext dbContext, MinistryAccess access, List<FollowUp> followUps, IReadOnlyDictionary<Guid, DirectoryUser> directory, CancellationToken cancellationToken)
    {
        var attendeeIds = followUps.Where(item => item.SubjectAttendeeId != null).Select(item => item.SubjectAttendeeId!.Value).Distinct().ToList();
        var groups = await dbContext.GroupMemberships
            .Where(membership => membership.EndedAt == null && attendeeIds.Contains(membership.AttendeeId))
            .Select(membership => new { membership.AttendeeId, Name = membership.Group.Generation.Name + " · " + membership.Group.Name })
            .ToDictionaryAsync(item => item.AttendeeId, item => item.Name, cancellationToken);
        var meetingGroupIds = followUps.Where(item => item.GroupId != null).Select(item => item.GroupId!.Value).Distinct().ToList();
        var meetingGroups = await dbContext.Groups.IgnoreQueryFilters()
            .Where(group => meetingGroupIds.Contains(group.Id))
            .Select(group => new { group.Id, Name = group.Generation.Name + " · " + group.Name })
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);

        string? GroupLabel(FollowUp followUp, Guid attendeeId) =>
            followUp.GroupId is { } groupId ? meetingGroups.GetValueOrDefault(groupId) : groups.GetValueOrDefault(attendeeId);

        return followUps.Select(followUp =>
        {
            var subject = followUp.SubjectAttendee is { } attendee
                ? new FollowUpSubject { Type = PersonType.Attendee, Id = attendee.Id, FullName = attendee.FullName, Context = GroupLabel(followUp, attendee.Id) }
                : new FollowUpSubject
                {
                    Type = PersonType.User,
                    Id = followUp.SubjectUserId!.Value,
                    FullName = directory.NameOf(followUp.SubjectUserId.Value),
                    Context = RoleLabels.Of(directory.GetValueOrDefault(followUp.SubjectUserId.Value)?.Role)
                };

            return new FollowUpListItem
            {
                Id = followUp.Id,
                Subject = subject,
                AuthorId = followUp.AuthorUserId,
                AuthorName = directory.NameOf(followUp.CreatedBy),
                Status = followUp.Status,
                ScheduledDate = followUp.ScheduledDate,
                ScheduledTime = followUp.ScheduledTime,
                MeetingDate = followUp.MeetingDate,
                IsConfidential = followUp.IsConfidential,
                CanEdit = access.CanEditFollowUp(followUp)
            };
        }).ToList();
    }
}
