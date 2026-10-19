using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Mappings;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Queries;

/// <summary>Asistentes del alcance: el director ve todos; los demás, los de sus grupos.</summary>
public class GetAttendeesQuery : IRequest<PagedResult<AttendeeListItem>>
{
    public string? Search { get; set; }
    public Guid? GenerationId { get; set; }
    public Guid? GroupId { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsServer { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class GetAttendeesQueryHandler : IRequestHandler<GetAttendeesQuery, PagedResult<AttendeeListItem>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;

    public GetAttendeesQueryHandler(IApplicationDbContext dbContext, MinistryAccess access)
    {
        _dbContext = dbContext;
        _access = access;
    }

    public async Task<PagedResult<AttendeeListItem>> Handle(GetAttendeesQuery request, CancellationToken cancellationToken = default)
    {
        var attendees = await _access.AttendeesInScopeAsync(_dbContext.Attendees, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpperInvariant();
            attendees = attendees.Where(attendee => (attendee.FirstName + " " + attendee.LastName).ToUpper().Contains(search)
                || (attendee.PhoneNumber != null && attendee.PhoneNumber.Contains(search)));
        }

        if (request.GroupId is not null)
        {
            attendees = attendees.Where(attendee => attendee.Memberships.Any(membership => membership.EndedAt == null && membership.GroupId == request.GroupId));
        }

        if (request.GenerationId is not null)
        {
            attendees = attendees.Where(attendee => attendee.Memberships.Any(membership => membership.EndedAt == null && membership.Group.GenerationId == request.GenerationId));
        }

        if (request.IsActive is not null)
        {
            attendees = attendees.Where(attendee => attendee.IsActive == request.IsActive);
        }

        if (request.IsServer is not null)
        {
            attendees = attendees.Where(attendee => attendee.IsServer == request.IsServer);
        }

        return await attendees
            .OrderBy(attendee => attendee.FirstName).ThenBy(attendee => attendee.LastName).ThenBy(attendee => attendee.Id)
            .Select(attendee => new AttendeeListItem
            {
                Id = attendee.Id,
                FullName = attendee.FirstName + " " + attendee.LastName,
                Group = attendee.Memberships.Where(membership => membership.EndedAt == null)
                    .Select(membership => new GroupRef
                    {
                        Id = membership.GroupId,
                        Name = membership.Group.Name,
                        Year = membership.Group.Year,
                        GenerationId = membership.Group.GenerationId,
                        GenerationName = membership.Group.Generation.Name,
                        GenerationProfile = membership.Group.Generation.Profile
                    })
                    .FirstOrDefault(),
                IsServer = attendee.IsServer,
                IsActive = attendee.IsActive
            })
            .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}

public class GetAttendeesQueryValidator : AbstractValidator<GetAttendeesQuery>
{
    public GetAttendeesQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Search).MaximumLength(100);
    }
}
