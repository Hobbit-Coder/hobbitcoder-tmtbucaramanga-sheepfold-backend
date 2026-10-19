using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Retired.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Retired.Queries;

/// <summary>Registros retirados que el usuario administra: el director, los ministeriales; el administrador, los de catálogos.</summary>
public class GetRetiredRecordsQuery : IRequest<List<RetiredRecordResponse>>
{
    public RetiredType? Type { get; set; }
}

public class GetRetiredRecordsQueryHandler : IRequestHandler<GetRetiredRecordsQuery, List<RetiredRecordResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;

    public GetRetiredRecordsQueryHandler(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
    }

    public async Task<List<RetiredRecordResponse>> Handle(GetRetiredRecordsQuery request, CancellationToken cancellationToken = default)
    {
        var records = new List<RetiredRecordResponse>();
        bool Wants(RetiredType type) => request.Type is null || request.Type == type;

        if (_access.IsDirector)
        {
            if (Wants(RetiredType.Attendee))
            {
                var attendees = await _dbContext.Attendees.IgnoreQueryFilters().Where(item => item.IsDeleted)
                    .Select(item => new
                    {
                        item.Id,
                        Label = item.FirstName + " " + item.LastName,
                        item.DeletedAt,
                        GroupActive = item.Memberships.Any(membership => membership.EndedAt == null && membership.Group.IsActive && !membership.Group.IsDeleted)
                    })
                    .ToListAsync(cancellationToken);
                records.AddRange(attendees.Select(item => new RetiredRecordResponse { Type = RetiredType.Attendee, Id = item.Id, Label = item.Label, State = "Retirado", RetiredAt = item.DeletedAt, CanRestore = true, RestoreNeedsGroup = !item.GroupActive }));
            }

            if (Wants(RetiredType.Group))
            {
                records.AddRange(await _dbContext.Groups.IgnoreQueryFilters().Where(item => item.IsDeleted)
                    .Select(item => new RetiredRecordResponse
                    {
                        Type = RetiredType.Group,
                        Id = item.Id,
                        Label = item.Generation.Name + " · " + item.Name + " · " + item.Year,
                        State = item.IsActive ? "Retirado" : "Inactivo",
                        RetiredAt = item.DeletedAt,
                        CanRestore = true,
                        RestoreNeedsGroup = false
                    })
                    .ToListAsync(cancellationToken));
            }

            if (Wants(RetiredType.Event))
            {
                records.AddRange(await _dbContext.Events.IgnoreQueryFilters().Where(item => item.IsDeleted)
                    .Select(item => new RetiredRecordResponse { Type = RetiredType.Event, Id = item.Id, Label = item.Title, State = item.Status.ToString(), RetiredAt = item.DeletedAt, CanRestore = true, RestoreNeedsGroup = false })
                    .ToListAsync(cancellationToken));
            }

            if (Wants(RetiredType.FollowUp))
            {
                var directory = await _userDirectory.GetUsersAsync(cancellationToken);
                var followUps = await _dbContext.FollowUps.IgnoreQueryFilters().Where(item => item.IsDeleted && item.SubjectUserId != _access.UserId)
                    .Select(item => new { item.Id, item.SubjectUserId, Attendee = item.SubjectAttendee == null ? null : item.SubjectAttendee.FirstName + " " + item.SubjectAttendee.LastName, item.DeletedAt })
                    .ToListAsync(cancellationToken);
                records.AddRange(followUps.Select(item => new RetiredRecordResponse
                {
                    Type = RetiredType.FollowUp,
                    Id = item.Id,
                    Label = $"Seguimiento · {item.Attendee ?? directory.NameOf(item.SubjectUserId!.Value)}",
                    State = "Retirado",
                    RetiredAt = item.DeletedAt,
                    CanRestore = true,
                    RestoreNeedsGroup = false
                }));
            }
        }
        else if (_access.IsAdmin)
        {
            records.AddRange(await Catalog(_dbContext.Generations, RetiredType.Generation, Wants, cancellationToken));
            records.AddRange(await Catalog(_dbContext.EventTypes, RetiredType.EventType, Wants, cancellationToken));
            records.AddRange(await Catalog(_dbContext.FollowUpTopics, RetiredType.FollowUpTopic, Wants, cancellationToken));
            records.AddRange(await Catalog(_dbContext.Courses, RetiredType.Course, Wants, cancellationToken));
        }
        else
        {
            throw new ForbiddenAccessException();
        }

        return records.OrderByDescending(record => record.RetiredAt).ThenBy(record => record.Label).ToList();
    }

    private static async Task<IEnumerable<RetiredRecordResponse>> Catalog<T>(IQueryable<T> items, RetiredType type, Func<RetiredType, bool> wants, CancellationToken cancellationToken)
        where T : CatalogItem =>
        !wants(type) ? [] : await items.Where(item => !item.IsActive)
            .Select(item => new RetiredRecordResponse { Type = type, Id = item.Id, Label = item.Name, State = "Inactivo", RetiredAt = item.UpdatedDate, CanRestore = true, RestoreNeedsGroup = false })
            .ToListAsync(cancellationToken);
}
