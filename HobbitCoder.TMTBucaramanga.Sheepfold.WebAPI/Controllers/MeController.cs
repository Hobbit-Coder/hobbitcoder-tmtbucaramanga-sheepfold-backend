using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Context.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Context.Queries;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class MeController : ControllerBase
{
    private readonly IMediator _mediator;

    public MeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Rol, capacidades y responsabilidades del usuario para la interfaz.</summary>
    [HttpGet("context")]
    [ProducesResponseType<MyContextResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MyContextResponse>> GetContextAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetMyContextQuery, MyContextResponse>(new GetMyContextQuery(), cancellationToken);
        return Ok(result);
    }
}
