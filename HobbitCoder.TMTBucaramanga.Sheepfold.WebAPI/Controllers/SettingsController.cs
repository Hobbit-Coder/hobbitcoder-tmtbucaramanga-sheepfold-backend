using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Commands;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Queries;
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
public class SettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SettingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("indicators")]
    [ProducesResponseType<OperationalSettingsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OperationalSettingsDto>> GetIndicatorsAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetOperationalSettingsQuery, OperationalSettingsDto>(new GetOperationalSettingsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("indicators")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateIndicatorsAsync([FromBody] UpdateOperationalSettingsCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send<UpdateOperationalSettingsCommand, Unit>(command, cancellationToken);
        return NoContent();
    }

    [HttpGet("notifications")]
    [ProducesResponseType<NotificationOptionsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationOptionsDto>> GetNotificationsAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetNotificationOptionsQuery, NotificationOptionsDto>(new GetNotificationOptionsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("notifications")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateNotificationsAsync([FromBody] UpdateNotificationOptionsCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send<UpdateNotificationOptionsCommand, Unit>(command, cancellationToken);
        return NoContent();
    }
}
