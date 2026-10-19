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
public class CatalogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CatalogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType<CatalogsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CatalogsResponse>> GetAsync([FromQuery] GetCatalogsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetCatalogsQuery, CatalogsResponse>(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    ///     Crea un elemento del catálogo.
    /// </summary>
    /// <remarks>
    ///     No existe GET por id para un elemento del catálogo: CreatedAtAction no tiene a dónde apuntar, así que se
    ///     conserva Created con la ruta actual para no cambiar la cabecera Location.
    /// </remarks>
    [HttpPost("{kind}")]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreateAsync([FromRoute] CatalogKind kind, [FromBody] CreateCatalogItemCommand command, CancellationToken cancellationToken)
    {
        command.Kind = kind;
        var id = await _mediator.Send<CreateCatalogItemCommand, Guid>(command, cancellationToken);
        return Created($"/api/catalogs/{kind}/{id}", id);
    }

    [HttpPut("{kind}/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync([FromRoute] CatalogKind kind, [FromRoute] Guid id, [FromBody] UpdateCatalogItemCommand command, CancellationToken cancellationToken)
    {
        command.Kind = kind;
        command.Id = id;
        await _mediator.Send<UpdateCatalogItemCommand, Unit>(command, cancellationToken);
        return NoContent();
    }
}
