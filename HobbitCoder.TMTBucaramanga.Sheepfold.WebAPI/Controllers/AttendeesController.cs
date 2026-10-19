using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Commands;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Queries;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Models;
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
public class AttendeesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AttendeesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<AttendeeListItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AttendeeListItem>>> GetAsync([FromQuery] GetAttendeesQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetAttendeesQuery, PagedResult<AttendeeListItem>>(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AttendeeProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendeeProfileResponse>> GetByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetAttendeeQuery, AttendeeProfileResponse>(new GetAttendeeQuery { Id = id }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType<List<MembershipHistoryItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<MembershipHistoryItem>>> GetHistoryAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetAttendeeHistoryQuery, List<MembershipHistoryItem>>(new GetAttendeeHistoryQuery { Id = id }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/activity")]
    [ProducesResponseType<AttendeeActivityResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendeeActivityResponse>> GetActivityAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetAttendeeActivityQuery, AttendeeActivityResponse>(new GetAttendeeActivityQuery { Id = id }, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreateAsync([FromBody] CreateAttendeeCommand command, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send<CreateAttendeeCommand, Guid>(command, cancellationToken);
        return CreatedAtAction("GetById", new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync([FromRoute] Guid id, [FromBody] UpdateAttendeeCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        await _mediator.Send<UpdateAttendeeCommand, Unit>(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/transfer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> TransferAsync([FromRoute] Guid id, [FromBody] TransferAttendeeCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        await _mediator.Send<TransferAttendeeCommand, Unit>(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RetireAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send<RetireAttendeeCommand, Unit>(new RetireAttendeeCommand { Id = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/contacts")]
    [ProducesResponseType<Guid>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> AddContactAsync([FromRoute] Guid id, [FromBody] ContactData contact, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<AddContactCommand, Guid>(new AddContactCommand { AttendeeId = id, Contact = contact }, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/contacts/{contactId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateContactAsync([FromRoute] Guid id, [FromRoute] Guid contactId, [FromBody] ContactData contact, CancellationToken cancellationToken)
    {
        await _mediator.Send<UpdateContactCommand, Unit>(new UpdateContactCommand { AttendeeId = id, ContactId = contactId, Contact = contact }, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/contacts/{contactId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveContactAsync([FromRoute] Guid id, [FromRoute] Guid contactId, CancellationToken cancellationToken)
    {
        await _mediator.Send<RemoveContactCommand, Unit>(new RemoveContactCommand { AttendeeId = id, ContactId = contactId }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/courses")]
    [ProducesResponseType<Guid>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> AddCourseAsync([FromRoute] Guid id, [FromBody] AddCourseCommand command, CancellationToken cancellationToken)
    {
        command.AttendeeId = id;
        var result = await _mediator.Send<AddCourseCommand, Guid>(command, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/courses/{attendeeCourseId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCourseAsync([FromRoute] Guid id, [FromRoute] Guid attendeeCourseId, [FromBody] UpdateCourseCommand command, CancellationToken cancellationToken)
    {
        command.AttendeeId = id;
        command.AttendeeCourseId = attendeeCourseId;
        await _mediator.Send<UpdateCourseCommand, Unit>(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/courses/{attendeeCourseId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveCourseAsync([FromRoute] Guid id, [FromRoute] Guid attendeeCourseId, CancellationToken cancellationToken)
    {
        await _mediator.Send<RemoveCourseCommand, Unit>(new RemoveCourseCommand { AttendeeId = id, AttendeeCourseId = attendeeCourseId }, cancellationToken);
        return NoContent();
    }
}
