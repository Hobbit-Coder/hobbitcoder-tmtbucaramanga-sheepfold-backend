using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Queries;

/// <summary>Seguimientos visibles: pendientes o realizados, con datos generales.</summary>
public class GetFollowUpsQuery : IRequest<PagedResult<FollowUpListItem>>
{
    public FollowUpStatus Status { get; set; } = FollowUpStatus.Pending;
    public Guid? GroupId { get; set; }
    public string? Search { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }

    /// <summary>Solo pendientes sin fecha programada.</summary>
    public bool? WithoutDate { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class GetFollowUpsQueryHandler : IRequestHandler<GetFollowUpsQuery, PagedResult<FollowUpListItem>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;

    public GetFollowUpsQueryHandler(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
    }

    public async Task<PagedResult<FollowUpListItem>> Handle(GetFollowUpsQuery request, CancellationToken cancellationToken = default)
    {
        FollowUpQueryHelpers.EnsurePastoralAccess(_access);
        var directory = await _userDirectory.GetUsersAsync(cancellationToken);
        var followUps = (await _access.VisibleFollowUpsAsync(_dbContext.FollowUps, directory, cancellationToken))
            .Where(followUp => followUp.Status == request.Status);

        if (request.GroupId is not null)
        {
            followUps = followUps.InGroup(request.GroupId.Value);
        }

        if (request.WithoutDate == true)
        {
            followUps = followUps.Where(followUp => followUp.ScheduledDate == null);
        }

        if (request.From is not null)
        {
            followUps = followUps.Where(followUp => (followUp.MeetingDate ?? followUp.ScheduledDate) >= request.From);
        }

        if (request.To is not null)
        {
            followUps = followUps.Where(followUp => (followUp.MeetingDate ?? followUp.ScheduledDate) <= request.To);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpperInvariant();
            var matchingUsers = directory.Values.Where(user => user.FullName.ToUpperInvariant().Contains(search)).Select(user => user.Id).ToList();
            followUps = followUps.Where(followUp =>
                (followUp.SubjectAttendee != null && (followUp.SubjectAttendee.FirstName + " " + followUp.SubjectAttendee.LastName).ToUpper().Contains(search))
                || (followUp.SubjectUserId != null && matchingUsers.Contains(followUp.SubjectUserId.Value)));
        }

        var ordered = request.Status == FollowUpStatus.Pending
            ? followUps.OrderBy(followUp => followUp.ScheduledDate == null).ThenBy(followUp => followUp.ScheduledDate).ThenBy(followUp => followUp.CreatedDate)
            : followUps.OrderByDescending(followUp => followUp.MeetingDate).ThenByDescending(followUp => followUp.CreatedDate);

        var total = await ordered.CountAsync(cancellationToken);
        var page = await ordered.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .Include(followUp => followUp.SubjectAttendee)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var items = await FollowUpQueryHelpers.ToListItemsAsync(_dbContext, _access, page, directory, cancellationToken);
        return new PagedResult<FollowUpListItem>(items, total, request.PageNumber, request.PageSize);
    }
}

public class GetFollowUpsQueryValidator : AbstractValidator<GetFollowUpsQuery>
{
    public GetFollowUpsQueryValidator()
    {
        RuleFor(query => query.Status).IsInEnum();
        RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Search).MaximumLength(100);
    }
}
