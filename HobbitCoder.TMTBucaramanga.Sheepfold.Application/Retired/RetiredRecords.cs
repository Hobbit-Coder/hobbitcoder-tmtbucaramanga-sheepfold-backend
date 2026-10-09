using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;
using ValidationException = HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Retired;

/// <summary>Tipos de registros retirados (§26). Los ministeriales son del director; los catálogos, del administrador (P05).</summary>
public enum RetiredType
{
    Attendee = 1,
    Group = 2,
    Event = 3,
    FollowUp = 4,
    Generation = 11,
    EventType = 12,
    FollowUpTopic = 13,
    Course = 14
}

public sealed record RetiredRecordResponse(RetiredType Type, Guid Id, string Label, string State, DateTime? RetiredAt, bool CanRestore, bool RestoreNeedsGroup);

/// <summary>Dependencias que se borrarían con el registro: solo conteos, nunca contenido (§26, S12).</summary>
public sealed record RetiredImpactResponse(RetiredType Type, Guid Id, string Label, IReadOnlyDictionary<string, int> Dependencies, bool CanDelete, string? BlockedReason);

/// <summary>Registros retirados que el usuario administra: el director, los ministeriales; el administrador, los de catálogos.</summary>
public class GetRetiredRecordsQuery : IRequest<List<RetiredRecordResponse>>
{
    public RetiredType? Type { get; set; }
}

public class GetRetiredImpactQuery : IRequest<RetiredImpactResponse>
{
    public RetiredType Type { get; set; }
    public Guid Id { get; set; }
}

/// <summary>
/// Restaura un registro retirado (decisión P05). Un asistente vuelve a las consultas con un grupo activo: si el suyo
/// ya no lo está, se indica <see cref="GroupId"/> y se traslada.
/// </summary>
public class RestoreRetiredRecordCommand : IRequest
{
    public RetiredType Type { get; set; }
    public Guid Id { get; set; }
    public Guid? GroupId { get; set; }
}

/// <summary>
/// Elimina definitivamente un registro previamente retirado (§26). La interfaz muestra antes su impacto y exige
/// confirmación. Los archivos de un evento se borran del almacenamiento después de confirmar el borrado en la base.
/// </summary>
public class DeleteRetiredRecordCommand : IRequest
{
    public RetiredType Type { get; set; }
    public Guid Id { get; set; }
}

public class RetiredRecordHandlers :
    IRequestHandler<GetRetiredRecordsQuery, List<RetiredRecordResponse>>,
    IRequestHandler<GetRetiredImpactQuery, RetiredImpactResponse>,
    IRequestHandler<RestoreRetiredRecordCommand, Unit>,
    IRequestHandler<DeleteRetiredRecordCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;
    private readonly MembershipService _memberships;
    private readonly FileStorageService _files;
    private readonly AuditTrail _audit;

    public RetiredRecordHandlers(
        IApplicationDbContext dbContext,
        MinistryAccess access,
        IUserDirectory userDirectory,
        MembershipService memberships,
        FileStorageService files,
        AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
        _memberships = memberships;
        _files = files;
        _audit = audit;
    }

    public async Task<List<RetiredRecordResponse>> Handle(GetRetiredRecordsQuery request, CancellationToken cancellationToken = default)
    {
        var records = new List<RetiredRecordResponse>();
        bool Wants(RetiredType type) => request.Type is null || request.Type == type;

        if (_access.IsDirector)
        {
            if (Wants(RetiredType.Attendee))
            {
                var attendees = await _dbContext.Attendees.IgnoreQueryFilters().Where(item => item.IsDeleted)
                    .Select(item => new
                    {
                        item.Id,
                        Label = item.FirstName + " " + item.LastName,
                        item.DeletedAt,
                        GroupActive = item.Memberships.Any(membership => membership.EndedAt == null && membership.Group.IsActive && !membership.Group.IsDeleted)
                    })
                    .ToListAsync(cancellationToken);
                records.AddRange(attendees.Select(item => new RetiredRecordResponse(RetiredType.Attendee, item.Id, item.Label, "Retirado", item.DeletedAt, true, !item.GroupActive)));
            }

            if (Wants(RetiredType.Group))
            {
                records.AddRange(await _dbContext.Groups.IgnoreQueryFilters().Where(item => item.IsDeleted)
                    .Select(item => new RetiredRecordResponse(RetiredType.Group, item.Id, item.Generation.Name + " · " + item.Name + " · " + item.Year,
                        item.IsActive ? "Retirado" : "Inactivo", item.DeletedAt, true, false))
                    .ToListAsync(cancellationToken));
            }

            if (Wants(RetiredType.Event))
            {
                records.AddRange(await _dbContext.Events.IgnoreQueryFilters().Where(item => item.IsDeleted)
                    .Select(item => new RetiredRecordResponse(RetiredType.Event, item.Id, item.Title, item.Status.ToString(), item.DeletedAt, true, false))
                    .ToListAsync(cancellationToken));
            }

            if (Wants(RetiredType.FollowUp))
            {
                var directory = await _userDirectory.GetUsersAsync(cancellationToken);
                var followUps = await _dbContext.FollowUps.IgnoreQueryFilters().Where(item => item.IsDeleted && item.SubjectUserId != _access.UserId)
                    .Select(item => new { item.Id, item.SubjectUserId, Attendee = item.SubjectAttendee == null ? null : item.SubjectAttendee.FirstName + " " + item.SubjectAttendee.LastName, item.DeletedAt })
                    .ToListAsync(cancellationToken);
                records.AddRange(followUps.Select(item => new RetiredRecordResponse(RetiredType.FollowUp, item.Id,
                    $"Seguimiento · {item.Attendee ?? directory.NameOf(item.SubjectUserId!.Value)}", "Retirado", item.DeletedAt, true, false)));
            }
        }
        else if (_access.IsAdmin)
        {
            records.AddRange(await Catalog(_dbContext.Generations, RetiredType.Generation, Wants, cancellationToken));
            records.AddRange(await Catalog(_dbContext.EventTypes, RetiredType.EventType, Wants, cancellationToken));
            records.AddRange(await Catalog(_dbContext.FollowUpTopics, RetiredType.FollowUpTopic, Wants, cancellationToken));
            records.AddRange(await Catalog(_dbContext.Courses, RetiredType.Course, Wants, cancellationToken));
        }
        else
        {
            throw new ForbiddenAccessException();
        }

        return records.OrderByDescending(record => record.RetiredAt).ThenBy(record => record.Label).ToList();
    }

    public async Task<RetiredImpactResponse> Handle(GetRetiredImpactQuery request, CancellationToken cancellationToken = default)
    {
        EnsureOwner(request.Type);
        var id = request.Id;

        switch (request.Type)
        {
            case RetiredType.Attendee:
                var attendee = await RetiredAttendeeAsync(id, cancellationToken);
                return Impact(request, attendee.FullName, null, new()
                {
                    ["groupHistory"] = await _dbContext.GroupMemberships.CountAsync(item => item.AttendeeId == id, cancellationToken),
                    ["attendanceMarks"] = await _dbContext.AttendeeAttendances.IgnoreQueryFilters().CountAsync(item => item.AttendeeId == id, cancellationToken),
                    ["followUps"] = await _dbContext.FollowUps.IgnoreQueryFilters().CountAsync(item => item.SubjectAttendeeId == id, cancellationToken),
                    ["contacts"] = await _dbContext.AttendeeContacts.CountAsync(item => item.AttendeeId == id, cancellationToken),
                    ["courses"] = await _dbContext.AttendeeCourses.CountAsync(item => item.AttendeeId == id, cancellationToken)
                });

            case RetiredType.Group:
                var group = await RetiredGroupAsync(id, cancellationToken);
                var memberships = await _dbContext.GroupMemberships.IgnoreQueryFilters().CountAsync(item => item.GroupId == id, cancellationToken);
                var events = await _dbContext.EventGroups.CountAsync(item => item.GroupId == id, cancellationToken);
                return Impact(request, group.Name,
                    memberships + events > 0 ? "The group is part of attendees' history or events and cannot be permanently deleted." : null,
                    new() { ["groupHistory"] = memberships, ["events"] = events, ["team"] = await _dbContext.GroupAssignments.CountAsync(item => item.GroupId == id, cancellationToken) });

            case RetiredType.Event:
                var @event = await RetiredEventAsync(id, cancellationToken);
                return Impact(request, @event.Title, null, new()
                {
                    ["attendanceMarks"] = await _dbContext.AttendeeAttendances.IgnoreQueryFilters().CountAsync(item => item.EventId == id, cancellationToken)
                        + await _dbContext.UserAttendances.IgnoreQueryFilters().CountAsync(item => item.EventId == id, cancellationToken),
                    ["comments"] = await _dbContext.EventComments.IgnoreQueryFilters().CountAsync(item => item.EventId == id, cancellationToken),
                    ["files"] = await _dbContext.EventAttachments.IgnoreQueryFilters().CountAsync(item => item.EventId == id, cancellationToken)
                        + await _dbContext.EventCommentAttachments.IgnoreQueryFilters().CountAsync(item => item.Comment.EventId == id, cancellationToken)
                });

            case RetiredType.FollowUp:
                await RetiredFollowUpAsync(id, cancellationToken);
                return Impact(request, "Seguimiento", null, new());

            default:
                var (label, references) = await CatalogReferencesAsync(request.Type, id, cancellationToken);
                return Impact(request, label, references > 0 ? "The value is used by existing records and cannot be permanently deleted." : null,
                    new() { ["references"] = references });
        }
    }

    public async Task<Unit> Handle(RestoreRetiredRecordCommand request, CancellationToken cancellationToken = default)
    {
        EnsureOwner(request.Type);

        switch (request.Type)
        {
            case RetiredType.Attendee:
                await RestoreAttendeeAsync(request, cancellationToken);
                break;
            case RetiredType.Group:
                var group = await RetiredGroupAsync(request.Id, cancellationToken);
                Undelete(group);
                _audit.Record(AuditModule.Groups, AuditActions.Restored, group.Id, group.Name, null, new { group.Name, group.Year });
                break;
            case RetiredType.Event:
                var @event = await RetiredEventAsync(request.Id, cancellationToken);
                Undelete(@event);
                _audit.Record(AuditModule.Events, AuditActions.Restored, @event.Id, @event.Title, null, new { @event.Title, @event.Status });
                break;
            case RetiredType.FollowUp:
                var followUp = await RetiredFollowUpAsync(request.Id, cancellationToken);
                Undelete(followUp);
                _audit.Record(AuditModule.FollowUps, AuditActions.Restored, followUp.Id, "Seguimiento", null, null);
                break;
            default:
                var item = await FindCatalogAsync(request.Type, request.Id, cancellationToken);
                item.IsActive = true;
                _audit.Record(AuditModule.Configuration, AuditActions.Restored, item.Id, $"{request.Type}: {item.Name}", null, new { item.Name });
                break;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    public async Task<Unit> Handle(DeleteRetiredRecordCommand request, CancellationToken cancellationToken = default)
    {
        var impact = await Handle(new GetRetiredImpactQuery { Type = request.Type, Id = request.Id }, cancellationToken);

        if (!impact.CanDelete)
        {
            throw new ConflictException(impact.BlockedReason!);
        }

        var objectKeys = new List<string>();

        switch (request.Type)
        {
            case RetiredType.Attendee:
                var attendee = await RetiredAttendeeAsync(request.Id, cancellationToken);
                // La pertenencia vigente no admite borrado en cascada por su índice filtrado: se borra con el asistente.
                _dbContext.Attendees.Remove(attendee);
                _audit.Record(AuditModule.Attendees, AuditActions.PermanentlyDeleted, attendee.Id, attendee.FullName, impact.Dependencies, null);
                break;
            case RetiredType.Group:
                var group = await RetiredGroupAsync(request.Id, cancellationToken);
                _dbContext.Groups.Remove(group);
                _audit.Record(AuditModule.Groups, AuditActions.PermanentlyDeleted, group.Id, group.Name, impact.Dependencies, null);
                break;
            case RetiredType.Event:
                var @event = await RetiredEventAsync(request.Id, cancellationToken);
                objectKeys.AddRange(await _dbContext.EventAttachments.IgnoreQueryFilters().Where(item => item.EventId == @event.Id).Select(item => item.ObjectKey).ToListAsync(cancellationToken));
                objectKeys.AddRange(await _dbContext.EventCommentAttachments.IgnoreQueryFilters().Where(item => item.Comment.EventId == @event.Id).Select(item => item.ObjectKey).ToListAsync(cancellationToken));
                _dbContext.Events.Remove(@event);
                _audit.Record(AuditModule.Events, AuditActions.PermanentlyDeleted, @event.Id, @event.Title, impact.Dependencies, null);
                break;
            case RetiredType.FollowUp:
                var followUp = await RetiredFollowUpAsync(request.Id, cancellationToken);
                _dbContext.FollowUps.Remove(followUp);
                _audit.Record(AuditModule.FollowUps, AuditActions.PermanentlyDeleted, followUp.Id, "Seguimiento", null, null);
                break;
            default:
                var item = await FindCatalogAsync(request.Type, request.Id, cancellationToken);
                RemoveCatalog(item);
                _audit.Record(AuditModule.Configuration, AuditActions.PermanentlyDeleted, item.Id, $"{request.Type}: {item.Name}", new { item.Name }, null);
                break;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Los objetos se borran solo después de confirmar el borrado de sus metadatos.
        await _files.CompensateAsync(objectKeys);

        return Unit.Value;
    }

    private async Task RestoreAttendeeAsync(RestoreRetiredRecordCommand request, CancellationToken cancellationToken)
    {
        var attendee = await RetiredAttendeeAsync(request.Id, cancellationToken);
        var currentGroupActive = await _dbContext.GroupMemberships.IgnoreQueryFilters()
            .AnyAsync(membership => membership.AttendeeId == attendee.Id && membership.EndedAt == null && membership.Group.IsActive && !membership.Group.IsDeleted, cancellationToken);

        Undelete(attendee);
        _audit.Record(AuditModule.Attendees, AuditActions.Restored, attendee.Id, attendee.FullName, null, new { attendee.FirstName, attendee.LastName });

        if (currentGroupActive)
        {
            return;
        }

        if (request.GroupId is null)
        {
            throw ValidationException.For(nameof(request.GroupId), "Select an active group for the attendee.");
        }

        var group = await _memberships.FindActiveGroupAsync(request.GroupId.Value, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _memberships.TransferAsync(attendee, group, null, cancellationToken);
    }

    private void EnsureOwner(RetiredType type)
    {
        var isCatalog = type >= RetiredType.Generation;

        if (isCatalog ? !_access.IsAdmin : !_access.IsDirector)
        {
            throw new ForbiddenAccessException();
        }
    }

    private static RetiredImpactResponse Impact(GetRetiredImpactQuery request, string label, string? blockedReason, Dictionary<string, int> dependencies) =>
        new(request.Type, request.Id, label, dependencies, blockedReason is null, blockedReason);

    private static void Undelete(Domain.Entities.Common.ISoftDeleteableEntity entity)
    {
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
    }

    private async Task<Attendee> RetiredAttendeeAsync(Guid id, CancellationToken cancellationToken) =>
        await _dbContext.Attendees.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted, cancellationToken)
        ?? throw new NotFoundException(nameof(Attendee), id);

    private async Task<Group> RetiredGroupAsync(Guid id, CancellationToken cancellationToken) =>
        await _dbContext.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted, cancellationToken)
        ?? throw new NotFoundException(nameof(Group), id);

    private async Task<Event> RetiredEventAsync(Guid id, CancellationToken cancellationToken) =>
        await _dbContext.Events.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted, cancellationToken)
        ?? throw new NotFoundException(nameof(Event), id);

    /// <summary>El director nunca recibe un seguimiento del que es la persona atendida (§28.12).</summary>
    private async Task<FollowUp> RetiredFollowUpAsync(Guid id, CancellationToken cancellationToken)
    {
        var me = _access.UserId;
        return await _dbContext.FollowUps.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted && item.SubjectUserId != me, cancellationToken)
            ?? throw new NotFoundException(nameof(FollowUp), id);
    }

    private static async Task<IEnumerable<RetiredRecordResponse>> Catalog<T>(IQueryable<T> items, RetiredType type, Func<RetiredType, bool> wants, CancellationToken cancellationToken)
        where T : CatalogItem =>
        !wants(type) ? [] : await items.Where(item => !item.IsActive)
            .Select(item => new RetiredRecordResponse(type, item.Id, item.Name, "Inactivo", item.UpdatedDate, true, false))
            .ToListAsync(cancellationToken);

    private async Task<CatalogItem> FindCatalogAsync(RetiredType type, Guid id, CancellationToken cancellationToken)
    {
        CatalogItem? item = type switch
        {
            RetiredType.Generation => await _dbContext.Generations.FirstOrDefaultAsync(entity => entity.Id == id && !entity.IsActive, cancellationToken),
            RetiredType.EventType => await _dbContext.EventTypes.FirstOrDefaultAsync(entity => entity.Id == id && !entity.IsActive, cancellationToken),
            RetiredType.FollowUpTopic => await _dbContext.FollowUpTopics.FirstOrDefaultAsync(entity => entity.Id == id && !entity.IsActive, cancellationToken),
            _ => await _dbContext.Courses.FirstOrDefaultAsync(entity => entity.Id == id && !entity.IsActive, cancellationToken)
        };

        return item ?? throw new NotFoundException(type.ToString(), id);
    }

    private async Task<(string Label, int References)> CatalogReferencesAsync(RetiredType type, Guid id, CancellationToken cancellationToken)
    {
        var item = await FindCatalogAsync(type, id, cancellationToken);
        var references = type switch
        {
            RetiredType.Generation => await _dbContext.Groups.IgnoreQueryFilters().CountAsync(group => group.GenerationId == id, cancellationToken)
                + await _dbContext.EventGenerations.CountAsync(link => link.GenerationId == id, cancellationToken),
            RetiredType.EventType => await _dbContext.Events.IgnoreQueryFilters().CountAsync(@event => @event.EventTypeId == id, cancellationToken),
            RetiredType.FollowUpTopic => await _dbContext.FollowUps.IgnoreQueryFilters().CountAsync(followUp => followUp.TopicId == id, cancellationToken),
            _ => await _dbContext.AttendeeCourses.CountAsync(course => course.CourseId == id, cancellationToken)
        };

        return (item.Name, references);
    }

    private void RemoveCatalog(CatalogItem item)
    {
        switch (item)
        {
            case Generation generation: _dbContext.Generations.Remove(generation); break;
            case EventType eventType: _dbContext.EventTypes.Remove(eventType); break;
            case FollowUpTopic topic: _dbContext.FollowUpTopics.Remove(topic); break;
            case Course course: _dbContext.Courses.Remove(course); break;
        }
    }
}

public class RestoreRetiredRecordCommandValidator : AbstractValidator<RestoreRetiredRecordCommand>
{
    public RestoreRetiredRecordCommandValidator()
    {
        RuleFor(command => command.Type).IsInEnum();
        RuleFor(command => command.Id).NotEmpty();
    }
}
