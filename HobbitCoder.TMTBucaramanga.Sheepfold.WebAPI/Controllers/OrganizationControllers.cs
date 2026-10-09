using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Context;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Controllers;

/// <summary>
/// Todos los controladores exigen un token de Identity para esta API. La autorización por rol, grupo, autoría y
/// encargo se aplica en cada handler con <c>MinistryAccess</c>: estos controladores solo traducen HTTP.
/// </summary>
[ApiController]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public abstract class SheepfoldControllerBase : ControllerBase
{
    protected SheepfoldControllerBase(IMediator mediator)
    {
        Mediator = mediator;
    }

    protected IMediator Mediator { get; }

    protected Task<TResponse> Send<TRequest, TResponse>(TRequest request) where TRequest : IRequest<TResponse> =>
        Mediator.Send<TRequest, TResponse>(request, HttpContext.RequestAborted);

    protected async Task<IActionResult> NoContentAfter<TRequest>(TRequest request) where TRequest : IRequest<Unit>
    {
        await Mediator.Send<TRequest, Unit>(request, HttpContext.RequestAborted);
        return NoContent();
    }
}

[Route("api/me")]
public class MeController : SheepfoldControllerBase
{
    public MeController(IMediator mediator) : base(mediator)
    {
    }

    /// <summary>Rol, capacidades y responsabilidades del usuario para la interfaz.</summary>
    [HttpGet("context")]
    public Task<MyContextResponse> GetContextAsync() => Send<GetMyContextQuery, MyContextResponse>(new GetMyContextQuery());
}

[Route("api/users")]
public class UserResponsibilitiesController : SheepfoldControllerBase
{
    public UserResponsibilitiesController(IMediator mediator) : base(mediator)
    {
    }

    /// <summary>Responsabilidades en grupos de una cuenta, para la administración de usuarios (administrador y director).</summary>
    [HttpGet("{userId:guid}/responsibilities")]
    public Task<List<MyGroupResponse>> GetAsync(Guid userId) =>
        Send<GetUserResponsibilitiesQuery, List<MyGroupResponse>>(new GetUserResponsibilitiesQuery { UserId = userId });
}

[Route("api/catalogs")]
public class CatalogsController : SheepfoldControllerBase
{
    public CatalogsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public Task<CatalogsResponse> GetAsync([FromQuery] GetCatalogsQuery query) => Send<GetCatalogsQuery, CatalogsResponse>(query);

    [HttpPost("{kind}")]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateAsync(CatalogKind kind, [FromBody] CreateCatalogItemCommand command)
    {
        command.Kind = kind;
        var id = await Send<CreateCatalogItemCommand, Guid>(command);
        return Created($"/api/catalogs/{kind}/{id}", id);
    }

    [HttpPut("{kind}/{id:guid}")]
    public Task<IActionResult> UpdateAsync(CatalogKind kind, Guid id, [FromBody] UpdateCatalogItemCommand command)
    {
        command.Kind = kind;
        command.Id = id;
        return NoContentAfter(command);
    }
}

[Route("api/settings")]
public class SettingsController : SheepfoldControllerBase
{
    public SettingsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet("indicators")]
    public Task<OperationalSettingsDto> GetIndicatorsAsync() => Send<GetOperationalSettingsQuery, OperationalSettingsDto>(new GetOperationalSettingsQuery());

    [HttpPut("indicators")]
    public Task<IActionResult> UpdateIndicatorsAsync([FromBody] UpdateOperationalSettingsCommand command) => NoContentAfter(command);

    [HttpGet("notifications")]
    public Task<NotificationOptionsDto> GetNotificationsAsync() => Send<GetNotificationOptionsQuery, NotificationOptionsDto>(new GetNotificationOptionsQuery());

    [HttpPut("notifications")]
    public Task<IActionResult> UpdateNotificationsAsync([FromBody] UpdateNotificationOptionsCommand command) => NoContentAfter(command);
}

[Route("api/groups")]
public class GroupsController : SheepfoldControllerBase
{
    public GroupsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public Task<List<GroupSummaryResponse>> GetAsync([FromQuery] GetGroupsQuery query) => Send<GetGroupsQuery, List<GroupSummaryResponse>>(query);

    [HttpGet("{id:guid}")]
    public Task<GroupDetailResponse> GetByIdAsync(Guid id) => Send<GetGroupByIdQuery, GroupDetailResponse>(new GetGroupByIdQuery { Id = id });

    [HttpGet("{id:guid}/history")]
    public Task<List<GroupHistoryEntry>> GetHistoryAsync(Guid id) => Send<GetGroupHistoryQuery, List<GroupHistoryEntry>>(new GetGroupHistoryQuery { Id = id });

    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateAsync([FromBody] CreateGroupCommand command)
    {
        var id = await Send<CreateGroupCommand, Guid>(command);
        return Created($"/api/groups/{id}", id);
    }

    [HttpPut("{id:guid}")]
    public Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateGroupCommand command)
    {
        command.Id = id;
        return NoContentAfter(command);
    }

    [HttpPut("{id:guid}/team")]
    public Task<IActionResult> SetTeamAsync(Guid id, [FromBody] SetGroupTeamCommand command)
    {
        command.Id = id;
        return NoContentAfter(command);
    }

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> RetireAsync(Guid id) => NoContentAfter(new RetireGroupCommand { Id = id });

    /// <summary>Cuentas activas que pueden integrar un equipo (director y coordinadores).</summary>
    [HttpGet("team-candidates")]
    public Task<List<TeamCandidate>> GetTeamCandidatesAsync() => Send<GetTeamCandidatesQuery, List<TeamCandidate>>(new GetTeamCandidatesQuery());

    [HttpGet("renewal")]
    public Task<RenewalOverviewResponse> GetRenewalAsync([FromQuery] GetRenewalOverviewQuery query) =>
        Send<GetRenewalOverviewQuery, RenewalOverviewResponse>(query);

    [HttpPost("renewal/assignments")]
    public Task<IActionResult> SaveRenewalAsync([FromBody] SaveRenewalAssignmentsCommand command) => NoContentAfter(command);
}
