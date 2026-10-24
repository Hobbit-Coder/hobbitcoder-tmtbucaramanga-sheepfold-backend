using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Queries;

/// <summary>Ficha del asistente: datos actuales, contactos, cursos y advertencias.</summary>
public class GetAttendeeQuery : IRequest<AttendeeProfileResponse>
{
    public Guid Id { get; set; }
}

public class GetAttendeeQueryHandler : IRequestHandler<GetAttendeeQuery, AttendeeProfileResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly TimeProvider _timeProvider;

    public GetAttendeeQueryHandler(IApplicationDbContext dbContext, MinistryAccess access, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _access = access;
        _timeProvider = timeProvider;
    }

    public async Task<AttendeeProfileResponse> Handle(GetAttendeeQuery request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.Id, cancellationToken);

        var membership = await _dbContext.GroupMemberships
            .Include(item => item.Group).ThenInclude(group => group.Generation)
            .FirstOrDefaultAsync(item => item.AttendeeId == attendee.Id && item.EndedAt == null, cancellationToken);
        var generation = membership?.Group.Generation;

        var contacts = await _dbContext.AttendeeContacts
            .Where(contact => contact.AttendeeId == attendee.Id)
            .OrderBy(contact => contact.Kind).ThenBy(contact => contact.FullName)
            .Select(contact => new ContactResponse { Id = contact.Id, Kind = contact.Kind, FullName = contact.FullName, Relationship = contact.Relationship, PhoneNumber = contact.PhoneNumber })
            .ToListAsync(cancellationToken);

        var courses = await _dbContext.AttendeeCourses
            .Where(course => course.AttendeeId == attendee.Id)
            .OrderBy(course => course.Course.Name)
            .Select(course => new CourseResponse { Id = course.Id, CourseId = course.CourseId, CourseName = course.Course.Name, Status = course.Status, FinishedOn = course.FinishedOn })
            .ToListAsync(cancellationToken);

        var scope = await _access.GetScopeAsync(cancellationToken);
        var managesGroup = membership is not null && scope.ManagedGroupIds.Contains(membership.GroupId);

        return new AttendeeProfileResponse
        {
            Id = attendee.Id,
            FirstName = attendee.FirstName,
            LastName = attendee.LastName,
            Gender = attendee.Gender,
            PhoneNumber = attendee.PhoneNumber,
            Instagram = attendee.Instagram,
            BirthDate = attendee.BirthDate,
            Age = attendee.AgeOn(LocalCalendar.Today(_timeProvider)),
            EducationalStage = attendee.EducationalStage,
            IsServer = attendee.IsServer,
            IsActive = attendee.IsActive,
            Group = membership is null ? null : ToRef(membership.Group),
            RequiredContactKind = generation?.RequiredContactKind,
            GenerationWarning = generation is not null && attendee.EducationalStage is { } stage && !generation.MatchesStage(stage),
            MissingRequiredContact = generation is not null && !contacts.Any(contact => contact.Kind == generation.RequiredContactKind),
            Contacts = contacts,
            Courses = courses,
            Capabilities = new AttendeeCapabilities
            {
                Edit = true,
                Transfer = _access.CanRegisterAttendees || (scope.Role is UserRole.Coordinator or UserRole.Leader && managesGroup),
                FollowUp = scope.IsDirector || (_access.CanFollowUp && managesGroup),
                Retire = scope.IsDirector
            }
        };
    }

    private static GroupRef ToRef(Group group) =>
        new GroupRef { Id = group.Id, Name = group.Name, Year = group.Year, GenerationId = group.GenerationId, GenerationName = group.Generation.Name, GenerationProfile = group.Generation.Profile };
}
