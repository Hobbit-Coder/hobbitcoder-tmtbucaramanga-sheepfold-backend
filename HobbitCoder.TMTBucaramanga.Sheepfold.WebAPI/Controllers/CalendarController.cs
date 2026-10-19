using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Calendar.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Calendar.Queries;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class CalendarController : ControllerBase
{
    private readonly IMediator _mediator;

    public CalendarController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType<CalendarResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CalendarResponse>> GetAsync([FromQuery] GetCalendarQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetCalendarQuery, CalendarResponse>(query, cancellationToken);
        return Ok(result);
    }
}
