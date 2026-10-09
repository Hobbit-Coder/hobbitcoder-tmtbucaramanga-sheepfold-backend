using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps;

/// <summary>
/// Filtros por grupo o generación: un realizado pertenece al grupo de la reunión (<see cref="FollowUp.GroupId"/>);
/// un pendiente, al grupo actual del asistente. Así los registros no se reasignan al trasladar a la persona (HU-25).
/// </summary>
public static class FollowUpGroupFilters
{
    public static IQueryable<FollowUp> InGroup(this IQueryable<FollowUp> followUps, Guid groupId) =>
        followUps.Where(followUp => followUp.GroupId == groupId
            || (followUp.GroupId == null && followUp.SubjectAttendee!.Memberships.Any(membership => membership.EndedAt == null && membership.GroupId == groupId)));

    public static IQueryable<FollowUp> InGeneration(this IQueryable<FollowUp> followUps, Guid generationId) =>
        followUps.Where(followUp => (followUp.GroupId != null && followUp.Group!.GenerationId == generationId)
            || (followUp.GroupId == null && followUp.SubjectAttendee!.Memberships.Any(membership => membership.EndedAt == null && membership.Group.GenerationId == generationId)));
}
