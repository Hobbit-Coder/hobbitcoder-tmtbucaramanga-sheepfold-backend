using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Context.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Context.Queries;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class UserResponsibilitiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserResponsibilitiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Responsabilidades en grupos de una cuenta, para la administración de usuarios (administrador y director).</summary>
    [HttpGet("{userId:guid}/responsibilities")]
    [ProducesResponseType<List<MyGroupResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<MyGroupResponse>>> GetAsync([FromRoute] Guid userId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetUserResponsibilitiesQuery, List<MyGroupResponse>>(new GetUserResponsibilitiesQuery { UserId = userId }, cancellationToken);
        return Ok(result);
    }
}
