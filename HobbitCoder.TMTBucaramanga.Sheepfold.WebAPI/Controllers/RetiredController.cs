using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Retired.Commands;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Retired.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Retired.Queries;
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
public class RetiredController : ControllerBase
{
    private readonly IMediator _mediator;

    public RetiredController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType<List<RetiredRecordResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<RetiredRecordResponse>>> GetAsync([FromQuery] GetRetiredRecordsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetRetiredRecordsQuery, List<RetiredRecordResponse>>(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{type}/{id:guid}/impact")]
    [ProducesResponseType<RetiredImpactResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RetiredImpactResponse>> GetImpactAsync([FromRoute] RetiredType type, [FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetRetiredImpactQuery, RetiredImpactResponse>(new GetRetiredImpactQuery { Type = type, Id = id }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{type}/{id:guid}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RestoreAsync([FromRoute] RetiredType type, [FromRoute] Guid id, [FromBody] RestoreRetiredRecordCommand? command, CancellationToken cancellationToken)
    {
        command ??= new RestoreRetiredRecordCommand();
        command.Type = type;
        command.Id = id;
        await _mediator.Send<RestoreRetiredRecordCommand, Unit>(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{type}/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync([FromRoute] RetiredType type, [FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send<DeleteRetiredRecordCommand, Unit>(new DeleteRetiredRecordCommand { Type = type, Id = id }, cancellationToken);
        return NoContent();
    }
}
