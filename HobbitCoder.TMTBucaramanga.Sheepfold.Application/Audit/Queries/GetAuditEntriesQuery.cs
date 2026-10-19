using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Audit.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Audit.Queries;

/// <summary>Auditoría ministerial: solo directores y de solo lectura.</summary>
public class GetAuditEntriesQuery : IRequest<PagedResult<AuditEntryResponse>>
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public string? ActorUserId { get; set; }
    public AuditModule? Module { get; set; }
    public string? Action { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class GetAuditEntriesQueryHandler : IRequestHandler<GetAuditEntriesQuery, PagedResult<AuditEntryResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;

    public GetAuditEntriesQueryHandler(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
    }

    public async Task<PagedResult<AuditEntryResponse>> Handle(GetAuditEntriesQuery request, CancellationToken cancellationToken = default)
    {
        _access.EnsureDirector();
        var entries = _dbContext.AuditEntries.AsNoTracking();

        if (request.From is not null)
        {
            var from = request.From.Value.UtcDateTime;
            entries = entries.Where(entry => entry.OccurredAt >= from);
        }

        if (request.To is not null)
        {
            var to = request.To.Value.UtcDateTime;
            entries = entries.Where(entry => entry.OccurredAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(request.ActorUserId))
        {
            entries = entries.Where(entry => entry.ActorUserId == request.ActorUserId);
        }

        if (request.Module is not null)
        {
            entries = entries.Where(entry => entry.Module == request.Module);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            entries = entries.Where(entry => entry.Action == request.Action);
        }

        var total = await entries.CountAsync(cancellationToken);
        var page = await entries.OrderByDescending(entry => entry.OccurredAt).ThenByDescending(entry => entry.Id)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var directory = await _userDirectory.GetUsersAsync(cancellationToken);

        return new PagedResult<AuditEntryResponse>(page.Select(entry => AuditQueryHelpers.ToResponse(entry, directory)).ToList(), total, request.PageNumber, request.PageSize);
    }
}

public class GetAuditEntriesQueryValidator : AbstractValidator<GetAuditEntriesQuery>
{
    public GetAuditEntriesQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Module).IsInEnum();
        RuleFor(query => query.Action).MaximumLength(60);
    }
}
