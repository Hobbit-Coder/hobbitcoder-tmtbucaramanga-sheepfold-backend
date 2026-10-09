using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Calendar;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Controllers;

[Route("api/calendar")]
public class CalendarController : SheepfoldControllerBase
{
    public CalendarController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public Task<CalendarResponse> GetAsync([FromQuery] GetCalendarQuery query) => Send<GetCalendarQuery, CalendarResponse>(query);
}

[Route("api/events")]
public class EventsController : SheepfoldControllerBase
{
    public EventsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public Task<PagedResult<EventSummaryResponse>> GetAsync([FromQuery] GetEventsQuery query) => Send<GetEventsQuery, PagedResult<EventSummaryResponse>>(query);

    /// <summary>Públicos, generaciones, grupos y encargados que el usuario puede elegir.</summary>
    [HttpGet("options")]
    public Task<AudienceOptionsResponse> GetOptionsAsync() => Send<GetEventOptionsQuery, AudienceOptionsResponse>(new GetEventOptionsQuery());

    [HttpGet("{id:guid}")]
    public Task<EventDetailResponse> GetByIdAsync(Guid id) => Send<GetEventQuery, EventDetailResponse>(new GetEventQuery { Id = id });

    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateAsync([FromBody] CreateEventCommand command)
    {
        var id = await Send<CreateEventCommand, Guid>(command);
        return Created($"/api/events/{id}", id);
    }

    [HttpPut("{id:guid}")]
    public Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateEventCommand command)
    {
        command.Id = id;
        return NoContentAfter(command);
    }

    [HttpPost("{id:guid}/status")]
    public Task<IActionResult> ChangeStatusAsync(Guid id, [FromBody] ChangeEventStatusCommand command)
    {
        command.Id = id;
        return NoContentAfter(command);
    }

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> RetireAsync(Guid id) => NoContentAfter(new RetireEventCommand { Id = id });

    [HttpPost("{id:guid}/attachments")]
    public Task<FileResponse> AddAttachmentAsync(Guid id, IFormFile file) =>
        Send<AddEventAttachmentCommand, FileResponse>(new AddEventAttachmentCommand { EventId = id, File = ToUploadedFile(file) });

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachmentAsync(Guid id, Guid attachmentId) =>
        Download(await Send<GetEventAttachmentContentQuery, FileContent>(new GetEventAttachmentContentQuery { EventId = id, AttachmentId = attachmentId }));

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    public Task<IActionResult> RemoveAttachmentAsync(Guid id, Guid attachmentId) =>
        NoContentAfter(new RemoveEventAttachmentCommand { EventId = id, AttachmentId = attachmentId });

    [HttpGet("{id:guid}/comments")]
    public Task<List<CommentResponse>> GetCommentsAsync(Guid id) => Send<GetEventCommentsQuery, List<CommentResponse>>(new GetEventCommentsQuery { EventId = id });

    /// <summary>Comentario o respuesta con archivos opcionales (multipart: <c>body</c>, <c>parentCommentId</c>, <c>files</c>).</summary>
    [HttpPost("{id:guid}/comments")]
    public Task<CommentResponse> AddCommentAsync(Guid id, [FromForm] string body, [FromForm] Guid? parentCommentId, [FromForm] List<IFormFile>? files) =>
        Send<AddEventCommentCommand, CommentResponse>(new AddEventCommentCommand
        {
            EventId = id,
            Body = body,
            ParentCommentId = parentCommentId,
            Files = (files ?? []).Select(ToUploadedFile).ToList()
        });

    [HttpGet("{id:guid}/comments/{commentId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadCommentAttachmentAsync(Guid id, Guid commentId, Guid attachmentId) =>
        Download(await Send<GetCommentAttachmentContentQuery, FileContent>(new GetCommentAttachmentContentQuery
        {
            EventId = id,
            CommentId = commentId,
            AttachmentId = attachmentId
        }));

    [HttpGet("{id:guid}/attendance")]
    public Task<AttendanceSheetResponse> GetAttendanceAsync(Guid id, [FromQuery] Guid? groupId, [FromQuery] string? search) =>
        Send<GetAttendanceSheetQuery, AttendanceSheetResponse>(new GetAttendanceSheetQuery { EventId = id, GroupId = groupId, Search = search });

    /// <summary>Marca o quita la marca (<c>mark: null</c>) de una persona.</summary>
    [HttpPut("{id:guid}/attendance")]
    public Task<AttendancePerson> SetAttendanceAsync(Guid id, [FromBody] SetAttendanceMarkCommand command)
    {
        command.EventId = id;
        return Send<SetAttendanceMarkCommand, AttendancePerson>(command);
    }

    private static UploadedFile ToUploadedFile(IFormFile file) => new(file.FileName, file.ContentType, file.Length, file.OpenReadStream);

    /// <summary>
    /// Descarga como adjunto, nunca en línea: el navegador no interpreta archivos subidos por usuarios (con
    /// <c>X-Content-Type-Options: nosniff</c> de las cabeceras de seguridad).
    /// </summary>
    private FileStreamResult Download(FileContent content)
    {
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(content.Content, content.ContentType, content.FileName);
    }
}
