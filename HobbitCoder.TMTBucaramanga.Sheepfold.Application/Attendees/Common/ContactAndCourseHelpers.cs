using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;

public static class ContactAndCourseHelpers
{
    public static ContactData Snapshot(AttendeeContact contact) =>
        new() { Kind = contact.Kind, FullName = contact.FullName, Relationship = contact.Relationship, PhoneNumber = contact.PhoneNumber };

    public static async Task<AttendeeCourse> FindCourseAsync(IApplicationDbContext dbContext, Guid attendeeId, Guid attendeeCourseId, CancellationToken cancellationToken) =>
        await dbContext.AttendeeCourses.Include(course => course.Course)
            .FirstOrDefaultAsync(course => course.Id == attendeeCourseId && course.AttendeeId == attendeeId, cancellationToken)
        ?? throw new NotFoundException(nameof(AttendeeCourse), attendeeCourseId);

    /// <summary>Quitar o cambiar el tipo de un contacto no puede dejar la ficha sin el contacto que exige su generación.</summary>
    public static async Task EnsureRequirementKeptAsync(IApplicationDbContext dbContext, Guid attendeeId, AttendeeContact changing, CancellationToken cancellationToken)
    {
        var generation = await dbContext.GroupMemberships
            .Where(membership => membership.AttendeeId == attendeeId && membership.EndedAt == null)
            .Select(membership => membership.Group.Generation)
            .FirstOrDefaultAsync(cancellationToken);

        if (generation is null || changing.Kind != generation.RequiredContactKind)
        {
            return;
        }

        var remaining = await dbContext.AttendeeContacts.CountAsync(
            contact => contact.AttendeeId == attendeeId && contact.Id != changing.Id && contact.Kind == generation.RequiredContactKind, cancellationToken);

        if (remaining == 0)
        {
            throw new ConflictException("This is the only contact of the kind required by the generation. Add another one first.");
        }
    }

    public static async Task<AttendeeContact> FindContactAsync(IApplicationDbContext dbContext, Guid attendeeId, Guid contactId, CancellationToken cancellationToken) =>
        await dbContext.AttendeeContacts.FirstOrDefaultAsync(contact => contact.Id == contactId && contact.AttendeeId == attendeeId, cancellationToken)
        ?? throw new NotFoundException(nameof(AttendeeContact), contactId);
}
