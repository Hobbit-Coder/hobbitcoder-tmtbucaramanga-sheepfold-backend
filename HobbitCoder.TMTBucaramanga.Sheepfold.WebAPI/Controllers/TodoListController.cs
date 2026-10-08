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
public class TodoListController : ControllerBase
{
    private readonly IMediator _mediator;

    public TodoListController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType<List<TodoListResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<TodoListResponse>>> GetTodoListsAsync([FromQuery] GetTodoListsQuery query, CancellationToken cancellationToken)
    {
        var todoLists = await _mediator.Send<GetTodoListsQuery, List<TodoListResponse>>(query, cancellationToken);

        return Ok(todoLists);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TodoListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoListResponse>> GetTodoListByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var todoList = await _mediator.Send<GetTodoListByIdQuery, TodoListResponse>(new GetTodoListByIdQuery { Id = id }, cancellationToken);

        return Ok(todoList);
    }

    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateTodoListAsync([FromBody] CreateTodoListCommand command, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send<CreateTodoListCommand, Guid>(command, cancellationToken);

        return CreatedAtAction("GetTodoListById", new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTodoListAsync([FromRoute] Guid id, [FromBody] UpdateTodoListCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest();
        }

        await _mediator.Send<UpdateTodoListCommand, Unit>(command, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTodoListAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send<DeleteTodoListCommand, Unit>(new DeleteTodoListCommand { Id = id }, cancellationToken);

        return NoContent();
    }
}
