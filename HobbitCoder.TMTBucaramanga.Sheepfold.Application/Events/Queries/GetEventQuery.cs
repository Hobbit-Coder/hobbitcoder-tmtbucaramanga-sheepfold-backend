using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Queries;

public class GetEventQuery : IRequest<EventDetailResponse>
{
    public Guid Id { get; set; }
}

public class GetEventQueryHandler : IRequestHandler<GetEventQuery, EventDetailResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;

    public GetEventQueryHandler(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
    }

    public async Task<EventDetailResponse> Handle(GetEventQuery request, CancellationToken cancellationToken = default)
    {
        var @event = await _access.FindVisibleEventAsync(request.Id, cancellationToken);
        var directory = await _userDirectory.GetUsersAsync(cancellationToken);

        var summary = await _dbContext.Events.Where(item => item.Id == @event.Id).Select(EventQueryHelpers.ToSummary).FirstAsync(cancellationToken);
        var attachments = await _dbContext.EventAttachments
            .Where(attachment => attachment.EventId == @event.Id)
            .OrderBy(attachment => attachment.CreatedDate)
            .Select(attachment => new { attachment.Id, attachment.FileName, attachment.ContentType, attachment.SizeBytes, attachment.CreatedDate, attachment.CreatedBy })
            .ToListAsync(cancellationToken);

        var canAdminister = _access.CanAdministerEvent(@event);
        var scope = await _access.GetScopeAsync(cancellationToken);
        var attendanceOpen = @event.RequiresAttendance && @event.Status is EventStatus.Published or EventStatus.Finished;

        return new EventDetailResponse
        {
            Event = summary,
            Description = @event.Description,
            Creator = Person(directory, @event.CreatedBy),
            Managers = @event.Managers.Select(manager => Person(directory, manager.UserId.ToString())).OrderBy(person => person.FullName).ToList(),
            Attachments = attachments.Select(item => new FileResponse { Id = item.Id, FileName = item.FileName, ContentType = item.ContentType, SizeBytes = item.SizeBytes, UploadedAt = item.CreatedDate, UploadedBy = directory.NameOf(item.CreatedBy) }).ToList(),
            AllowedTransitions = canAdminister ? EventTransitions.From(@event.Status) : [],
            Capabilities = new EventCapabilities
            {
                Edit = canAdminister,
                ChangeStatus = canAdminister,
                Retire = canAdminister,
                Attendance = attendanceOpen && (canAdminister || scope.OperationalGroupIds.Count > 0),
                Comment = true
            }
        };
    }

    private static PersonRef Person(IReadOnlyDictionary<Guid, DirectoryUser> directory, string userId) =>
        Guid.TryParse(userId, out var id) && directory.TryGetValue(id, out var user)
            ? new PersonRef { Id = user.Id, FullName = user.FullName, Role = user.Role }
            : new PersonRef { Id = Guid.Empty, FullName = directory.NameOf(userId), Role = null };
}
