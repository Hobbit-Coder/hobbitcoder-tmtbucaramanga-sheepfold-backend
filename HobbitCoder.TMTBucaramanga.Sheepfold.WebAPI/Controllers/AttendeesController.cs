using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Controllers;

[Route("api/attendees")]
public class AttendeesController : SheepfoldControllerBase
{
    public AttendeesController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public Task<PagedResult<AttendeeListItem>> GetAsync([FromQuery] GetAttendeesQuery query) => Send<GetAttendeesQuery, PagedResult<AttendeeListItem>>(query);

    [HttpGet("{id:guid}")]
    public Task<AttendeeProfileResponse> GetByIdAsync(Guid id) => Send<GetAttendeeQuery, AttendeeProfileResponse>(new GetAttendeeQuery { Id = id });

    [HttpGet("{id:guid}/history")]
    public Task<List<MembershipHistoryItem>> GetHistoryAsync(Guid id) =>
        Send<GetAttendeeHistoryQuery, List<MembershipHistoryItem>>(new GetAttendeeHistoryQuery { Id = id });

    [HttpGet("{id:guid}/activity")]
    public Task<AttendeeActivityResponse> GetActivityAsync(Guid id) =>
        Send<GetAttendeeActivityQuery, AttendeeActivityResponse>(new GetAttendeeActivityQuery { Id = id });

    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateAsync([FromBody] CreateAttendeeCommand command)
    {
        var id = await Send<CreateAttendeeCommand, Guid>(command);
        return Created($"/api/attendees/{id}", id);
    }

    [HttpPut("{id:guid}")]
    public Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateAttendeeCommand command)
    {
        command.Id = id;
        return NoContentAfter(command);
    }

    [HttpPost("{id:guid}/transfer")]
    public Task<IActionResult> TransferAsync(Guid id, [FromBody] TransferAttendeeCommand command)
    {
        command.Id = id;
        return NoContentAfter(command);
    }

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> RetireAsync(Guid id) => NoContentAfter(new RetireAttendeeCommand { Id = id });

    [HttpPost("{id:guid}/contacts")]
    public Task<Guid> AddContactAsync(Guid id, [FromBody] ContactData contact) =>
        Send<AddContactCommand, Guid>(new AddContactCommand { AttendeeId = id, Contact = contact });

    [HttpPut("{id:guid}/contacts/{contactId:guid}")]
    public Task<IActionResult> UpdateContactAsync(Guid id, Guid contactId, [FromBody] ContactData contact) =>
        NoContentAfter(new UpdateContactCommand { AttendeeId = id, ContactId = contactId, Contact = contact });

    [HttpDelete("{id:guid}/contacts/{contactId:guid}")]
    public Task<IActionResult> RemoveContactAsync(Guid id, Guid contactId) =>
        NoContentAfter(new RemoveContactCommand { AttendeeId = id, ContactId = contactId });

    [HttpPost("{id:guid}/courses")]
    public Task<Guid> AddCourseAsync(Guid id, [FromBody] AddCourseCommand command)
    {
        command.AttendeeId = id;
        return Send<AddCourseCommand, Guid>(command);
    }

    [HttpPut("{id:guid}/courses/{attendeeCourseId:guid}")]
    public Task<IActionResult> UpdateCourseAsync(Guid id, Guid attendeeCourseId, [FromBody] UpdateCourseCommand command)
    {
        command.AttendeeId = id;
        command.AttendeeCourseId = attendeeCourseId;
        return NoContentAfter(command);
    }

    [HttpDelete("{id:guid}/courses/{attendeeCourseId:guid}")]
    public Task<IActionResult> RemoveCourseAsync(Guid id, Guid attendeeCourseId) =>
        NoContentAfter(new RemoveCourseCommand { AttendeeId = id, AttendeeCourseId = attendeeCourseId });
}
