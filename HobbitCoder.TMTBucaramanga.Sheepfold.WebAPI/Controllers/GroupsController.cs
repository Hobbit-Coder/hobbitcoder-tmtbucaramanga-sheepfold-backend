using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Commands;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Queries;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class GroupsController : ControllerBase
{
    private readonly IMediator _mediator;

    public GroupsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType<List<GroupSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<GroupSummaryResponse>>> GetAsync([FromQuery] GetGroupsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetGroupsQuery, List<GroupSummaryResponse>>(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<GroupDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupDetailResponse>> GetByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetGroupByIdQuery, GroupDetailResponse>(new GetGroupByIdQuery { Id = id }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType<List<GroupHistoryEntry>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<GroupHistoryEntry>>> GetHistoryAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetGroupHistoryQuery, List<GroupHistoryEntry>>(new GetGroupHistoryQuery { Id = id }, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreateAsync([FromBody] CreateGroupCommand command, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send<CreateGroupCommand, Guid>(command, cancellationToken);
        return CreatedAtAction("GetById", new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync([FromRoute] Guid id, [FromBody] UpdateGroupCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        await _mediator.Send<UpdateGroupCommand, Unit>(command, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/team")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetTeamAsync([FromRoute] Guid id, [FromBody] SetGroupTeamCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        await _mediator.Send<SetGroupTeamCommand, Unit>(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RetireAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send<RetireGroupCommand, Unit>(new RetireGroupCommand { Id = id }, cancellationToken);
        return NoContent();
    }

    /// <summary>Cuentas activas que pueden integrar un equipo (director y coordinadores).</summary>
    [HttpGet("team-candidates")]
    [ProducesResponseType<List<TeamCandidate>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TeamCandidate>>> GetTeamCandidatesAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetTeamCandidatesQuery, List<TeamCandidate>>(new GetTeamCandidatesQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Grupos activos a los que el usuario puede registrar o trasladar asistentes.</summary>
    [HttpGet("destinations")]
    [ProducesResponseType<List<DestinationGroupResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DestinationGroupResponse>>> GetDestinationsAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetDestinationGroupsQuery, List<DestinationGroupResponse>>(new GetDestinationGroupsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("renewal")]
    [ProducesResponseType<RenewalOverviewResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RenewalOverviewResponse>> GetRenewalAsync([FromQuery] GetRenewalOverviewQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetRenewalOverviewQuery, RenewalOverviewResponse>(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("renewal/assignments")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SaveRenewalAsync([FromBody] SaveRenewalAssignmentsCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send<SaveRenewalAssignmentsCommand, Unit>(command, cancellationToken);
        return NoContent();
    }
}
