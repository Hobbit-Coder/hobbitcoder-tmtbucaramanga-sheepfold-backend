using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance;

public sealed record AttendanceGroupOption(Guid Id, string Name);

/// <param name="Unmarked">Personas sin marca: se interpretan como no asistió (§19).</param>
public sealed record AttendanceSummary(int Attended, int Late, int Unmarked);

public sealed record AttendanceSheetResponse(
    Guid EventId,
    EventStatus Status,
    IReadOnlyList<AttendanceGroupOption> Groups,
    IReadOnlyList<AttendancePerson> People,
    AttendanceSummary Summary);

/// <summary>Registro de asistencia del evento para el usuario (HU-17, HU-18).</summary>
public class GetAttendanceSheetQuery : IRequest<AttendanceSheetResponse>
{
    public Guid EventId { get; set; }
    public Guid? GroupId { get; set; }
    public string? Search { get; set; }
}

/// <summary>
/// Marca Asistió o Llegó tarde, o quita la marca con <see cref="Mark"/> nulo (HU-18, HU-19). Las dos marcas son
/// excluyentes: cambiar una reemplaza la otra. Sigue abierta después de finalizar el evento. Cada cambio se audita.
/// </summary>
public class SetAttendanceMarkCommand : IRequest<AttendancePerson>
{
    public Guid EventId { get; set; }
    public PersonType PersonType { get; set; }
    public Guid PersonId { get; set; }
    public AttendanceMark? Mark { get; set; }
}

public class AttendanceHandlers :
    IRequestHandler<GetAttendanceSheetQuery, AttendanceSheetResponse>,
    IRequestHandler<SetAttendanceMarkCommand, AttendancePerson>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AttendanceSheetService _sheet;
    private readonly AuditTrail _audit;

    public AttendanceHandlers(IApplicationDbContext dbContext, MinistryAccess access, AttendanceSheetService sheet, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _sheet = sheet;
        _audit = audit;
    }

    public async Task<AttendanceSheetResponse> Handle(GetAttendanceSheetQuery request, CancellationToken cancellationToken = default)
    {
        var @event = await FindOpenEventAsync(request.EventId, cancellationToken);
        var people = await _sheet.MarkablePeopleAsync(@event, cancellationToken);

        var groups = people.Where(person => person.GroupId is not null && person.Context is not null)
            .DistinctBy(person => person.GroupId)
            .Select(person => new AttendanceGroupOption(person.GroupId!.Value, person.Context!))
            .OrderBy(group => group.Name)
            .ToList();

        IEnumerable<AttendancePerson> filtered = people;

        if (request.GroupId is not null)
        {
            filtered = filtered.Where(person => person.GroupId == request.GroupId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            filtered = filtered.Where(person => person.FullName.Contains(request.Search.Trim(), StringComparison.CurrentCultureIgnoreCase));
        }

        var list = filtered.ToList();

        return new AttendanceSheetResponse(@event.Id, @event.Status, groups, list, new AttendanceSummary(
            list.Count(person => person.Mark == AttendanceMark.Attended),
            list.Count(person => person.Mark == AttendanceMark.Late),
            list.Count(person => person.Mark is null)));
    }

    public async Task<AttendancePerson> Handle(SetAttendanceMarkCommand request, CancellationToken cancellationToken = default)
    {
        var @event = await FindOpenEventAsync(request.EventId, cancellationToken);
        var person = (await _sheet.MarkablePeopleAsync(@event, cancellationToken))
            .FirstOrDefault(item => item.PersonType == request.PersonType && item.PersonId == request.PersonId)
            ?? throw new ForbiddenAccessException("The person is not part of your attendance list for this event.");

        if (person.Mark == request.Mark)
        {
            return person;
        }

        if (request.PersonType == PersonType.Attendee)
        {
            await ApplyAsync(_dbContext.AttendeeAttendances, item => item.EventId == @event.Id && item.AttendeeId == person.PersonId,
                () => new AttendeeAttendance { EventId = @event.Id, AttendeeId = person.PersonId, Mark = request.Mark!.Value }, request.Mark, cancellationToken);
        }
        else
        {
            await ApplyAsync(_dbContext.UserAttendances, item => item.EventId == @event.Id && item.UserId == person.PersonId,
                () => new UserAttendance { EventId = @event.Id, UserId = person.PersonId, Mark = request.Mark!.Value }, request.Mark, cancellationToken);
        }

        _audit.Record(AuditModule.Attendance, request.Mark is null ? AuditActions.AttendanceCleared : AuditActions.AttendanceMarked,
            @event.Id, $"{@event.Title} / {person.FullName}", new { person.Mark }, new { request.Mark });

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            // Otra persona registró la misma marca al mismo tiempo: se conserva la última confirmada (HU-19, caso 4).
            throw new ConflictException("The attendance changed while you were saving. Reload and try again.", exception);
        }

        return person with { Mark = request.Mark };
    }

    private static async Task ApplyAsync<T>(
        DbSet<T> set, System.Linq.Expressions.Expression<Func<T, bool>> key, Func<T> create, AttendanceMark? mark, CancellationToken cancellationToken)
        where T : class
    {
        var existing = await set.FirstOrDefaultAsync(key, cancellationToken);

        switch (existing, mark)
        {
            case (null, not null):
                set.Add(create());
                break;
            case (not null, null):
                set.Remove(existing);
                break;
            case (AttendeeAttendance attendee, { } value):
                attendee.Mark = value;
                break;
            case (UserAttendance user, { } value):
                user.Mark = value;
                break;
        }
    }

    /// <summary>
    /// El evento debe ser visible, requerir asistencia (§19) y estar publicado o finalizado: la asistencia sigue
    /// abierta para correcciones después de finalizar (§20).
    /// </summary>
    private async Task<Event> FindOpenEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var visible = await _access.VisibleEventsAsync(_dbContext.Events, cancellationToken);
        var @event = await visible.Include(item => item.Managers).Include(item => item.Groups).Include(item => item.Generations)
            .FirstOrDefaultAsync(item => item.Id == eventId, cancellationToken)
            ?? throw new NotFoundException(nameof(Event), eventId);

        if (!@event.RequiresAttendance || @event.Status is not (EventStatus.Published or EventStatus.Finished))
        {
            throw new ConflictException("This event does not have an open attendance record.");
        }

        return @event;
    }
}

public class SetAttendanceMarkCommandValidator : AbstractValidator<SetAttendanceMarkCommand>
{
    public SetAttendanceMarkCommandValidator()
    {
        RuleFor(command => command.PersonType).IsInEnum();
        RuleFor(command => command.PersonId).NotEmpty();
        RuleFor(command => command.Mark).IsInEnum().WithMessage("Mark must be Attended or Late");
    }
}
