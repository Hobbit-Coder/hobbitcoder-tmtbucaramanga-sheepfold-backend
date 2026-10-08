using HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Commands;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Queries;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Controllers;

/// <summary>
/// Exige un token de HobbitCoder.TMTBucaramanga.Identity emitido para esta API (audiencia
/// <c>HobbitCoder.TMTBucaramanga.Sheepfold</c>); sin él responde 401.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class TodoItemController : ControllerBase
{
    private readonly IMediator _mediator;

    public TodoItemController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType<List<TodoItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<TodoItemResponse>>> GetTodoItemsAsync([FromQuery] GetTodoItemsQuery query, CancellationToken cancellationToken)
    {
        var todoItems = await _mediator.Send<GetTodoItemsQuery, List<TodoItemResponse>>(query, cancellationToken);

        return Ok(todoItems);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TodoItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoItemResponse>> GetTodoItemByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var todoItem = await _mediator.Send<GetTodoItemByIdQuery, TodoItemResponse>(new GetTodoItemByIdQuery { Id = id }, cancellationToken);

        return Ok(todoItem);
    }

    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateTodoItemAsync([FromBody] CreateTodoItemCommand command, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send<CreateTodoItemCommand, Guid>(command, cancellationToken);

        return CreatedAtAction("GetTodoItemById", new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTodoItemAsync([FromRoute] Guid id, [FromBody] UpdateTodoItemCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest();
        }

        await _mediator.Send<UpdateTodoItemCommand, Unit>(command, cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTodoItemStatusAsync([FromRoute] Guid id, [FromBody] UpdateTodoItemStatusCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest();
        }

        await _mediator.Send<UpdateTodoItemStatusCommand, Unit>(command, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTodoItemAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send<DeleteTodoItemCommand, Unit>(new DeleteTodoItemCommand { Id = id }, cancellationToken);

        return NoContent();
    }
}
