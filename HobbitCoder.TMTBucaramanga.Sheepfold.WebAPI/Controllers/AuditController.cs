using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Audit.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Audit.Queries;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class AuditController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<AuditEntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AuditEntryResponse>>> GetAsync([FromQuery] GetAuditEntriesQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetAuditEntriesQuery, PagedResult<AuditEntryResponse>>(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AuditEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditEntryResponse>> GetByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetAuditEntryQuery, AuditEntryResponse>(new GetAuditEntryQuery { Id = id }, cancellationToken);
        return Ok(result);
    }
}
