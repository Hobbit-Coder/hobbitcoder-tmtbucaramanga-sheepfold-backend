using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Notifications.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;
using ValidationException = HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Commands;

/// <summary>
/// Publica un comentario o una respuesta, con archivos opcionales. Pueden participar los usuarios que
/// consultan el evento; una respuesta se relaciona con un comentario raíz (una sola indentación).
/// </summary>
public class AddEventCommentCommand : IRequest<CommentResponse>
{
    public Guid EventId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public required string Body { get; set; }
    public List<UploadedFile> Files { get; set; } = [];
}

public class AddEventCommentCommandHandler : IRequestHandler<AddEventCommentCommand, CommentResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly FileStorageService _files;
    private readonly IUserDirectory _userDirectory;
    private readonly NotificationDispatcher _notifications;

    public AddEventCommentCommandHandler(
        IApplicationDbContext dbContext,
        MinistryAccess access,
        FileStorageService files,
        IUserDirectory userDirectory,
        NotificationDispatcher notifications)
    {
        _dbContext = dbContext;
        _access = access;
        _files = files;
        _userDirectory = userDirectory;
        _notifications = notifications;
    }

    /// <summary>
    ///     Atiende &lt;see cref="AddEventCommentCommand"/&gt;.
    /// </summary>
    /// <remarks>
    ///     Una sola indentación: responder a una respuesta se relaciona con su comentario raíz.
    /// </remarks>
    public async Task<CommentResponse> Handle(AddEventCommentCommand request, CancellationToken cancellationToken = default)
    {
        var @event = await _access.FindVisibleEventAsync(request.EventId, cancellationToken);
        EventComment? parent = null;

        if (request.ParentCommentId is not null)
        {
            parent = await _dbContext.EventComments.FirstOrDefaultAsync(comment => comment.Id == request.ParentCommentId && comment.EventId == @event.Id, cancellationToken)
                ?? throw ValidationException.For(nameof(request.ParentCommentId), "The comment to reply to does not exist.");

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
        return new CommentResponse
        {
            Id = comment.Id,
            ParentCommentId = comment.ParentCommentId,
            Author = EventCollaborationHelpers.Author(directory, comment.CreatedBy),
            Body = comment.Body,
            CreatedAt = comment.CreatedDate,
            Attachments = attachments.Select(item => new FileResponse { Id = item.Id, FileName = item.FileName, ContentType = item.ContentType, SizeBytes = item.SizeBytes, UploadedAt = comment.CreatedDate, UploadedBy = directory.NameOf(comment.CreatedBy) }).ToList()
        };
    }

    /// <summary>
    /// avisa a quienes se relacionan con la conversación: creador, encargados y quienes ya comentaron en el
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
}

public class AddEventCommentCommandValidator : AbstractValidator<AddEventCommentCommand>
{
    public AddEventCommentCommandValidator()
    {
        RuleFor(command => command.Body).NotEmpty().WithMessage("Write a comment.").MaximumLength(4000);
        RuleFor(command => command.Files.Count).LessThanOrEqualTo(10).WithMessage("Attach at most 10 files.");
    }
}
