using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Commands;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Queries;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/follow-ups")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class FollowUpsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FollowUpsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<FollowUpListItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<FollowUpListItem>>> GetAsync([FromQuery] GetFollowUpsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetFollowUpsQuery, PagedResult<FollowUpListItem>>(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("subjects")]
    [ProducesResponseType<List<FollowUpSubject>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<FollowUpSubject>>> GetSubjectsAsync([FromQuery] GetFollowUpSubjectsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetFollowUpSubjectsQuery, List<FollowUpSubject>>(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<FollowUpDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FollowUpDetailResponse>> GetByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetFollowUpQuery, FollowUpDetailResponse>(new GetFollowUpQuery { Id = id }, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreateAsync([FromBody] CreateFollowUpCommand command, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send<CreateFollowUpCommand, Guid>(command, cancellationToken);
        return CreatedAtAction("GetById", new { id }, id);
    }

    [HttpPut("{id:guid}/schedule")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ScheduleAsync([FromRoute] Guid id, [FromBody] ScheduleFollowUpCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        await _mediator.Send<ScheduleFollowUpCommand, Unit>(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompleteAsync([FromRoute] Guid id, [FromBody] FollowUpContent content, CancellationToken cancellationToken)
    {
        await _mediator.Send<CompleteFollowUpCommand, Unit>(new CompleteFollowUpCommand { Id = id, Content = content }, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/content")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateContentAsync([FromRoute] Guid id, [FromBody] FollowUpContent content, CancellationToken cancellationToken)
    {
        await _mediator.Send<UpdateFollowUpContentCommand, Unit>(new UpdateFollowUpContentCommand { Id = id, Content = content }, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RetireAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send<RetireFollowUpCommand, Unit>(new RetireFollowUpCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
