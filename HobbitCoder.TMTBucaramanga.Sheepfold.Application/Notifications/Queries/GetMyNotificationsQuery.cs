using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Mappings;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Notifications.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Notifications.Queries;

/// <summary>
/// Avisos del usuario autenticado, del más reciente al más antiguo. La fuente no define estados de
/// lectura. Abrir el evento o la ficha vuelve a validar el permiso en su propio endpoint.
/// </summary>
public class GetMyNotificationsQuery : IRequest<PagedResult<NotificationResponse>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, PagedResult<NotificationResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;

    public GetMyNotificationsQueryHandler(IApplicationDbContext dbContext, MinistryAccess access)
    {
        _dbContext = dbContext;
        _access = access;
    }

    public Task<PagedResult<NotificationResponse>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken = default)
    {
        _access.EnsureMinistry();
        var userId = _access.UserId;

        return _dbContext.Notifications
            .Where(notification => notification.RecipientUserId == userId)
            .OrderByDescending(notification => notification.CreatedAt).ThenByDescending(notification => notification.Id)
            .Select(notification => new NotificationResponse
            {
                Id = notification.Id,
                Kind = notification.Kind,
                Title = notification.Title,
                Message = notification.Message,
                CreatedAt = notification.CreatedAt,
                EventId = notification.EventId,
                AttendeeId = notification.AttendeeId
            })
            .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}

public class GetMyNotificationsQueryValidator : AbstractValidator<GetMyNotificationsQuery>
{
    public GetMyNotificationsQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
