using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Notifications.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;
using ValidationException = HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events;

/// <summary>Adjunta un archivo al evento (§15). Quien administra el evento.</summary>
public class AddEventAttachmentCommand : IRequest<FileResponse>
{
    public Guid EventId { get; set; }
    public required UploadedFile File { get; set; }
}

/// <summary>Retira un adjunto del evento; el objeto se conserva hasta la eliminación definitiva (§26).</summary>
public class RemoveEventAttachmentCommand : IRequest
{
    public Guid EventId { get; set; }
    public Guid AttachmentId { get; set; }
}

/// <summary>Descarga autorizada: solo quien puede consultar el evento (HU-16).</summary>
public class GetEventAttachmentContentQuery : IRequest<FileContent>
{
    public Guid EventId { get; set; }
    public Guid AttachmentId { get; set; }
}

public class GetEventCommentsQuery : IRequest<List<CommentResponse>>
{
    public Guid EventId { get; set; }
}

/// <summary>
/// Publica un comentario o una respuesta, con archivos opcionales (§18). Pueden participar los usuarios que
/// consultan el evento; una respuesta se relaciona con un comentario raíz (una sola indentación).
/// </summary>
public class AddEventCommentCommand : IRequest<CommentResponse>
{
    public Guid EventId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public required string Body { get; set; }
    public List<UploadedFile> Files { get; set; } = [];
}

public class GetCommentAttachmentContentQuery : IRequest<FileContent>
{
    public Guid EventId { get; set; }
    public Guid CommentId { get; set; }
    public Guid AttachmentId { get; set; }
}

public class EventCollaborationHandlers :
    IRequestHandler<AddEventAttachmentCommand, FileResponse>,
    IRequestHandler<RemoveEventAttachmentCommand, Unit>,
    IRequestHandler<GetEventAttachmentContentQuery, FileContent>,
    IRequestHandler<GetEventCommentsQuery, List<CommentResponse>>,
    IRequestHandler<AddEventCommentCommand, CommentResponse>,
    IRequestHandler<GetCommentAttachmentContentQuery, FileContent>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly FileStorageService _files;
    private readonly IUserDirectory _userDirectory;
    private readonly NotificationDispatcher _notifications;
    private readonly AuditTrail _audit;

    public EventCollaborationHandlers(
        IApplicationDbContext dbContext,
        MinistryAccess access,
        FileStorageService files,
        IUserDirectory userDirectory,
        NotificationDispatcher notifications,
        AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _files = files;
        _userDirectory = userDirectory;
        _notifications = notifications;
        _audit = audit;
    }

    public async Task<FileResponse> Handle(AddEventAttachmentCommand request, CancellationToken cancellationToken = default)
    {
        var @event = await _access.FindVisibleEventAsync(request.EventId, cancellationToken);

        if (!_access.CanAdministerEvent(@event))
        {
            throw new ForbiddenAccessException();
        }

        var upload = (await _files.UploadAsync($"events/{@event.Id:N}", [request.File], cancellationToken))[0];
        var attachment = new EventAttachment
        {
            EventId = @event.Id,
            ObjectKey = upload.ObjectKey,
            FileName = upload.FileName,
            ContentType = upload.ContentType,
            SizeBytes = upload.SizeBytes
        };

        try
        {
            _dbContext.EventAttachments.Add(attachment);
            _audit.Record(AuditModule.Events, AuditActions.AttachmentAdded, @event.Id, $"{@event.Title} / {attachment.FileName}", null,
                new { attachment.FileName, attachment.ContentType, attachment.SizeBytes });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _files.CompensateAsync([upload.ObjectKey]);
            throw;
        }

        var directory = await _userDirectory.GetUsersAsync(cancellationToken);
        return new FileResponse(attachment.Id, attachment.FileName, attachment.ContentType, attachment.SizeBytes, attachment.CreatedDate, directory.NameOf(attachment.CreatedBy));
    }

    public async Task<Unit> Handle(RemoveEventAttachmentCommand request, CancellationToken cancellationToken = default)
    {
        var @event = await _access.FindVisibleEventAsync(request.EventId, cancellationToken);

        if (!_access.CanAdministerEvent(@event))
        {
            throw new ForbiddenAccessException();
        }

        var attachment = await _dbContext.EventAttachments.FirstOrDefaultAsync(item => item.Id == request.AttachmentId && item.EventId == @event.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EventAttachment), request.AttachmentId);

        _dbContext.EventAttachments.Remove(attachment);
        _audit.Record(AuditModule.Events, AuditActions.AttachmentRemoved, @event.Id, $"{@event.Title} / {attachment.FileName}",
            new { attachment.FileName, attachment.ContentType, attachment.SizeBytes }, null);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    public async Task<FileContent> Handle(GetEventAttachmentContentQuery request, CancellationToken cancellationToken = default)
    {
        var @event = await _access.FindVisibleEventAsync(request.EventId, cancellationToken);
        var attachment = await _dbContext.EventAttachments.FirstOrDefaultAsync(item => item.Id == request.AttachmentId && item.EventId == @event.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EventAttachment), request.AttachmentId);

        return new FileContent(await _files.OpenAsync(attachment.ObjectKey, cancellationToken), attachment.FileName, attachment.ContentType);
    }

    public async Task<List<CommentResponse>> Handle(GetEventCommentsQuery request, CancellationToken cancellationToken = default)
    {
        var @event = await _access.FindVisibleEventAsync(request.EventId, cancellationToken);
        var directory = await _userDirectory.GetUsersAsync(cancellationToken);

        var comments = await _dbContext.EventComments
            .Where(comment => comment.EventId == @event.Id)
            .OrderBy(comment => comment.CreatedDate)
            .Select(comment => new
            {
                comment.Id,
                comment.ParentCommentId,
                comment.CreatedBy,
                comment.Body,
                comment.CreatedDate,
                Attachments = comment.Attachments.Select(attachment => new { attachment.Id, attachment.FileName, attachment.ContentType, attachment.SizeBytes, attachment.CreatedDate }).ToList()
            })
            .ToListAsync(cancellationToken);

        return comments.Select(comment => new CommentResponse(
                comment.Id,
                comment.ParentCommentId,
                Author(directory, comment.CreatedBy),
                comment.Body,
                comment.CreatedDate,
                comment.Attachments.Select(item => new FileResponse(item.Id, item.FileName, item.ContentType, item.SizeBytes, item.CreatedDate, directory.NameOf(comment.CreatedBy))).ToList()))
            .ToList();
    }

    public async Task<CommentResponse> Handle(AddEventCommentCommand request, CancellationToken cancellationToken = default)
    {
        var @event = await _access.FindVisibleEventAsync(request.EventId, cancellationToken);
        EventComment? parent = null;

        if (request.ParentCommentId is not null)
        {
            parent = await _dbContext.EventComments.FirstOrDefaultAsync(comment => comment.Id == request.ParentCommentId && comment.EventId == @event.Id, cancellationToken)
                ?? throw ValidationException.For(nameof(request.ParentCommentId), "The comment to reply to does not exist.");

            // Una sola indentación: responder a una respuesta se relaciona con su comentario raíz.
            if (parent.ParentCommentId is not null)
            {
                parent = await _dbContext.EventComments.FirstAsync(comment => comment.Id == parent.ParentCommentId, cancellationToken);
            }
        }

        var comment = new EventComment { EventId = @event.Id, ParentCommentId = parent?.Id, Body = request.Body.Trim() };
        var uploads = await _files.UploadAsync($"events/{@event.Id:N}/comments/{comment.Id:N}", request.Files, cancellationToken);

        var attachments = uploads.Select(upload => new EventCommentAttachment
        {
            CommentId = comment.Id,
            ObjectKey = upload.ObjectKey,
            FileName = upload.FileName,
            ContentType = upload.ContentType,
            SizeBytes = upload.SizeBytes
        }).ToList();

        try
        {
            _dbContext.EventComments.Add(comment);
            _dbContext.EventCommentAttachments.AddRange(attachments);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _files.CompensateAsync(uploads.Select(upload => upload.ObjectKey));
            throw;
        }

        await NotifyParticipantsAsync(@event, parent, cancellationToken);

        var directory = await _userDirectory.GetUsersAsync(cancellationToken);
        return new CommentResponse(comment.Id, comment.ParentCommentId, Author(directory, comment.CreatedBy), comment.Body, comment.CreatedDate,
            attachments.Select(item => new FileResponse(item.Id, item.FileName, item.ContentType, item.SizeBytes, comment.CreatedDate, directory.NameOf(comment.CreatedBy))).ToList());
    }

    public async Task<FileContent> Handle(GetCommentAttachmentContentQuery request, CancellationToken cancellationToken = default)
    {
        var @event = await _access.FindVisibleEventAsync(request.EventId, cancellationToken);
        var attachment = await _dbContext.EventCommentAttachments.FirstOrDefaultAsync(item => item.Id == request.AttachmentId
                && item.CommentId == request.CommentId && item.Comment.EventId == @event.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EventCommentAttachment), request.AttachmentId);

        return new FileContent(await _files.OpenAsync(attachment.ObjectKey, cancellationToken), attachment.FileName, attachment.ContentType);
    }

    /// <summary>
    /// §18: avisa a quienes se relacionan con la conversación: creador, encargados y quienes ya comentaron en el
    /// hilo (o en el evento, si es un comentario nuevo). El aviso no incluye el texto del comentario.
    /// </summary>
    private async Task NotifyParticipantsAsync(Event @event, EventComment? parent, CancellationToken cancellationToken)
    {
        var participants = await _dbContext.EventComments
            .Where(comment => comment.EventId == @event.Id && (parent == null || comment.Id == parent.Id || comment.ParentCommentId == parent.Id))
            .Select(comment => comment.CreatedBy)
            .Distinct()
            .ToListAsync(cancellationToken);

        var recipients = participants.Append(@event.CreatedBy)
            .Select(id => Guid.TryParse(id, out var userId) ? userId : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Concat(@event.Managers.Select(manager => manager.UserId));

        await _notifications.NotifyAsync(recipients, NotificationKind.DiscussionActivity,
            parent is null ? $"Nuevo comentario en {@event.Title}" : $"Nueva respuesta en {@event.Title}",
            "Hay actividad nueva en la discusión del evento.", eventId: @event.Id, cancellationToken: cancellationToken);
    }

    private static PersonRef Author(IReadOnlyDictionary<Guid, DirectoryUser> directory, string userId) =>
        Guid.TryParse(userId, out var id) && directory.TryGetValue(id, out var user)
            ? new PersonRef(user.Id, user.FullName, user.Role)
            : new PersonRef(Guid.Empty, directory.NameOf(userId), null);
}

public class AddEventCommentCommandValidator : AbstractValidator<AddEventCommentCommand>
{
    public AddEventCommentCommandValidator()
    {
        RuleFor(command => command.Body).NotEmpty().WithMessage("Write a comment.").MaximumLength(4000);
        RuleFor(command => command.Files.Count).LessThanOrEqualTo(10).WithMessage("Attach at most 10 files.");
    }
}
