using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Reports.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Reports.Queries;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{family}")]
    [ProducesResponseType<ReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReportResponse>> GetAsync([FromRoute] ReportFamily family, [FromQuery] GetReportQuery query, CancellationToken cancellationToken)
    {
        query.Family = family;
        var result = await _mediator.Send<GetReportQuery, ReportResponse>(query, cancellationToken);
        return Ok(result);
    }
}
