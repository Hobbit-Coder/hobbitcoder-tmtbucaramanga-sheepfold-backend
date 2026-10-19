using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Commands;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Queries;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Commands;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Queries;
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
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<EventSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<EventSummaryResponse>>> GetAsync([FromQuery] GetEventsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetEventsQuery, PagedResult<EventSummaryResponse>>(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Públicos, generaciones, grupos y encargados que el usuario puede elegir.</summary>
    [HttpGet("options")]
    [ProducesResponseType<AudienceOptionsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AudienceOptionsResponse>> GetOptionsAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetEventOptionsQuery, AudienceOptionsResponse>(new GetEventOptionsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<EventDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailResponse>> GetByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetEventQuery, EventDetailResponse>(new GetEventQuery { Id = id }, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreateAsync([FromBody] CreateEventCommand command, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send<CreateEventCommand, Guid>(command, cancellationToken);
        return CreatedAtAction("GetById", new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync([FromRoute] Guid id, [FromBody] UpdateEventCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        await _mediator.Send<UpdateEventCommand, Unit>(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeStatusAsync([FromRoute] Guid id, [FromBody] ChangeEventStatusCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        await _mediator.Send<ChangeEventStatusCommand, Unit>(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RetireAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send<RetireEventCommand, Unit>(new RetireEventCommand { Id = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/attachments")]
    [ProducesResponseType<FileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileResponse>> AddAttachmentAsync([FromRoute] Guid id, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<AddEventAttachmentCommand, FileResponse>(
            new AddEventAttachmentCommand { EventId = id, File = ToUploadedFile(file) }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAttachmentAsync([FromRoute] Guid id, [FromRoute] Guid attachmentId, CancellationToken cancellationToken)
    {
        var content = await _mediator.Send<GetEventAttachmentContentQuery, FileContent>(
            new GetEventAttachmentContentQuery { EventId = id, AttachmentId = attachmentId }, cancellationToken);
        return Download(content);
    }

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAttachmentAsync([FromRoute] Guid id, [FromRoute] Guid attachmentId, CancellationToken cancellationToken)
    {
        await _mediator.Send<RemoveEventAttachmentCommand, Unit>(
            new RemoveEventAttachmentCommand { EventId = id, AttachmentId = attachmentId }, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/comments")]
    [ProducesResponseType<List<CommentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CommentResponse>>> GetCommentsAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetEventCommentsQuery, List<CommentResponse>>(new GetEventCommentsQuery { EventId = id }, cancellationToken);
        return Ok(result);
    }

    /// <summary>Comentario o respuesta con archivos opcionales (multipart: <c>body</c>, <c>parentCommentId</c>, <c>files</c>).</summary>
    [HttpPost("{id:guid}/comments")]
    [ProducesResponseType<CommentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentResponse>> AddCommentAsync(
        [FromRoute] Guid id,
        [FromForm] string body,
        [FromForm] Guid? parentCommentId,
        [FromForm] List<IFormFile>? files,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<AddEventCommentCommand, CommentResponse>(new AddEventCommentCommand
        {
            EventId = id,
            Body = body,
            ParentCommentId = parentCommentId,
            Files = (files ?? []).Select(ToUploadedFile).ToList()
        }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/comments/{commentId:guid}/attachments/{attachmentId:guid}")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadCommentAttachmentAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid commentId,
        [FromRoute] Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var content = await _mediator.Send<GetCommentAttachmentContentQuery, FileContent>(new GetCommentAttachmentContentQuery
        {
            EventId = id,
            CommentId = commentId,
            AttachmentId = attachmentId
        }, cancellationToken);
        return Download(content);
    }

    [HttpGet("{id:guid}/attendance")]
    [ProducesResponseType<AttendanceSheetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceSheetResponse>> GetAttendanceAsync(
        [FromRoute] Guid id,
        [FromQuery] Guid? groupId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send<GetAttendanceSheetQuery, AttendanceSheetResponse>(
            new GetAttendanceSheetQuery { EventId = id, GroupId = groupId, Search = search }, cancellationToken);
        return Ok(result);
    }

    /// <summary>Marca o quita la marca (<c>mark: null</c>) de una persona.</summary>
    [HttpPut("{id:guid}/attendance")]
    [ProducesResponseType<AttendancePerson>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AttendancePerson>> SetAttendanceAsync([FromRoute] Guid id, [FromBody] SetAttendanceMarkCommand command, CancellationToken cancellationToken)
    {
        command.EventId = id;
        var result = await _mediator.Send<SetAttendanceMarkCommand, AttendancePerson>(command, cancellationToken);
        return Ok(result);
    }

    private static UploadedFile ToUploadedFile(IFormFile file) => new UploadedFile { FileName = file.FileName, ContentType = file.ContentType, SizeBytes = file.Length, OpenReadStream = file.OpenReadStream };

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
