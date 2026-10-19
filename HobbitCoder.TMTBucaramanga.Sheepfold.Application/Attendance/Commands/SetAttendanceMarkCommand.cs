using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Commands;

/// <summary>
/// Marca Asistió o Llegó tarde, o quita la marca con <see cref="Mark"/> nulo. Las dos marcas son
/// excluyentes: cambiar una reemplaza la otra. Sigue abierta después de finalizar el evento. Cada cambio se audita.
/// </summary>
public class SetAttendanceMarkCommand : IRequest<AttendancePerson>
{
    public Guid EventId { get; set; }
    public PersonType PersonType { get; set; }
    public Guid PersonId { get; set; }
    public AttendanceMark? Mark { get; set; }
}

public class SetAttendanceMarkCommandHandler : IRequestHandler<SetAttendanceMarkCommand, AttendancePerson>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AttendanceSheetService _sheet;
    private readonly AuditTrail _audit;

    public SetAttendanceMarkCommandHandler(
        IApplicationDbContext dbContext,
        MinistryAccess access,
        AttendanceSheetService sheet,
        AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _sheet = sheet;
        _audit = audit;
    }

    /// <summary>
    ///     Atiende &lt;see cref="SetAttendanceMarkCommand"/&gt;.
    /// </summary>
    /// <remarks>
    ///     Otra persona registró la misma marca al mismo tiempo: se conserva la última confirmada.
    /// </remarks>
    public async Task<AttendancePerson> Handle(SetAttendanceMarkCommand request, CancellationToken cancellationToken = default)
    {
        var @event = await AttendanceHelpers.FindOpenEventAsync(_dbContext, _access, request.EventId, cancellationToken);
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
            throw new ConflictException("The attendance changed while you were saving. Reload and try again.", exception);
        }

        person.Mark = request.Mark;

        return person;
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
