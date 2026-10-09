using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;
using ValidationException = HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees;

/// <summary>Agrega un tutor o contacto de emergencia (§9, HU-09).</summary>
public class AddContactCommand : IRequest<Guid>
{
    public Guid AttendeeId { get; set; }
    public required ContactData Contact { get; set; }
}

public class UpdateContactCommand : IRequest
{
    public Guid AttendeeId { get; set; }
    public Guid ContactId { get; set; }
    public required ContactData Contact { get; set; }
}

/// <summary>Quita un contacto; no se puede quitar el último del tipo que exige la generación.</summary>
public class RemoveContactCommand : IRequest
{
    public Guid AttendeeId { get; set; }
    public Guid ContactId { get; set; }
}

/// <summary>Registra un curso del catálogo (§10, HU-10). Finalizado admite fecha opcional.</summary>
public class AddCourseCommand : IRequest<Guid>
{
    public Guid AttendeeId { get; set; }
    public Guid CourseId { get; set; }
    public CourseStatus Status { get; set; }
    public DateOnly? FinishedOn { get; set; }
}

public class UpdateCourseCommand : IRequest
{
    public Guid AttendeeId { get; set; }
    public Guid AttendeeCourseId { get; set; }
    public CourseStatus Status { get; set; }
    public DateOnly? FinishedOn { get; set; }
}

public class RemoveCourseCommand : IRequest
{
    public Guid AttendeeId { get; set; }
    public Guid AttendeeCourseId { get; set; }
}

public class ContactAndCourseHandlers :
    IRequestHandler<AddContactCommand, Guid>,
    IRequestHandler<UpdateContactCommand, Unit>,
    IRequestHandler<RemoveContactCommand, Unit>,
    IRequestHandler<AddCourseCommand, Guid>,
    IRequestHandler<UpdateCourseCommand, Unit>,
    IRequestHandler<RemoveCourseCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;

    public ContactAndCourseHandlers(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
    }

    public async Task<Guid> Handle(AddContactCommand request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.AttendeeId, cancellationToken);
        var contact = AttendeeCommandHandlers.ToEntity(attendee.Id, request.Contact);

        _dbContext.AttendeeContacts.Add(contact);
        _audit.Record(AuditModule.Attendees, AuditActions.ContactAdded, attendee.Id, attendee.FullName, null, request.Contact);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return contact.Id;
    }

    public async Task<Unit> Handle(UpdateContactCommand request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.AttendeeId, cancellationToken);
        var contact = await FindContactAsync(attendee.Id, request.ContactId, cancellationToken);

        if (contact.Kind != request.Contact.Kind)
        {
            await EnsureRequirementKeptAsync(attendee.Id, contact, cancellationToken);
        }

        var before = Snapshot(contact);
        contact.Kind = request.Contact.Kind;
        contact.FullName = request.Contact.FullName.Trim();
        contact.Relationship = request.Contact.Relationship.Trim();
        contact.PhoneNumber = request.Contact.PhoneNumber.Trim();

        _audit.Record(AuditModule.Attendees, AuditActions.ContactUpdated, attendee.Id, attendee.FullName, before, Snapshot(contact));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    public async Task<Unit> Handle(RemoveContactCommand request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.AttendeeId, cancellationToken);
        var contact = await FindContactAsync(attendee.Id, request.ContactId, cancellationToken);
        await EnsureRequirementKeptAsync(attendee.Id, contact, cancellationToken);

        _dbContext.AttendeeContacts.Remove(contact);
        _audit.Record(AuditModule.Attendees, AuditActions.ContactRemoved, attendee.Id, attendee.FullName, Snapshot(contact), null);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    public async Task<Guid> Handle(AddCourseCommand request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.AttendeeId, cancellationToken);
        var course = await _dbContext.Courses.FirstOrDefaultAsync(item => item.Id == request.CourseId && item.IsActive, cancellationToken)
            ?? throw ValidationException.For(nameof(request.CourseId), "Select a course from the catalog.");

        // Un curso figura una sola vez en la formación del asistente: su avance se actualiza en ese registro.
        if (await _dbContext.AttendeeCourses.AnyAsync(item => item.AttendeeId == attendee.Id && item.CourseId == course.Id, cancellationToken))
        {
            throw new ConflictException($"The attendee already has the course {course.Name}. Update its status instead.");
        }

        var attendeeCourse = new AttendeeCourse
        {
            AttendeeId = attendee.Id,
            CourseId = course.Id,
            Status = request.Status,
            FinishedOn = request.Status == CourseStatus.Finished ? request.FinishedOn : null
        };

        _dbContext.AttendeeCourses.Add(attendeeCourse);
        _audit.Record(AuditModule.Courses, AuditActions.CourseAdded, attendee.Id, $"{attendee.FullName} / {course.Name}", null,
            new { Course = course.Name, attendeeCourse.Status, attendeeCourse.FinishedOn });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return attendeeCourse.Id;
    }

    public async Task<Unit> Handle(UpdateCourseCommand request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.AttendeeId, cancellationToken);
        var attendeeCourse = await FindCourseAsync(attendee.Id, request.AttendeeCourseId, cancellationToken);

        var before = new { Course = attendeeCourse.Course.Name, attendeeCourse.Status, attendeeCourse.FinishedOn };
        attendeeCourse.Status = request.Status;
        attendeeCourse.FinishedOn = request.Status == CourseStatus.Finished ? request.FinishedOn : null;

        _audit.Record(AuditModule.Courses, AuditActions.CourseUpdated, attendee.Id, $"{attendee.FullName} / {attendeeCourse.Course.Name}", before,
            new { Course = attendeeCourse.Course.Name, attendeeCourse.Status, attendeeCourse.FinishedOn });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    public async Task<Unit> Handle(RemoveCourseCommand request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.AttendeeId, cancellationToken);
        var attendeeCourse = await FindCourseAsync(attendee.Id, request.AttendeeCourseId, cancellationToken);

        _dbContext.AttendeeCourses.Remove(attendeeCourse);
        _audit.Record(AuditModule.Courses, AuditActions.CourseRemoved, attendee.Id, $"{attendee.FullName} / {attendeeCourse.Course.Name}",
            new { Course = attendeeCourse.Course.Name, attendeeCourse.Status, attendeeCourse.FinishedOn }, null);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private async Task<AttendeeContact> FindContactAsync(Guid attendeeId, Guid contactId, CancellationToken cancellationToken) =>
        await _dbContext.AttendeeContacts.FirstOrDefaultAsync(contact => contact.Id == contactId && contact.AttendeeId == attendeeId, cancellationToken)
        ?? throw new NotFoundException(nameof(AttendeeContact), contactId);

    private async Task<AttendeeCourse> FindCourseAsync(Guid attendeeId, Guid attendeeCourseId, CancellationToken cancellationToken) =>
        await _dbContext.AttendeeCourses.Include(course => course.Course)
            .FirstOrDefaultAsync(course => course.Id == attendeeCourseId && course.AttendeeId == attendeeId, cancellationToken)
        ?? throw new NotFoundException(nameof(AttendeeCourse), attendeeCourseId);

    /// <summary>Quitar o cambiar el tipo de un contacto no puede dejar la ficha sin el contacto que exige su generación.</summary>
    private async Task EnsureRequirementKeptAsync(Guid attendeeId, AttendeeContact changing, CancellationToken cancellationToken)
    {
        var generation = await _dbContext.GroupMemberships
            .Where(membership => membership.AttendeeId == attendeeId && membership.EndedAt == null)
            .Select(membership => membership.Group.Generation)
            .FirstOrDefaultAsync(cancellationToken);

        if (generation is null || changing.Kind != generation.RequiredContactKind)
        {
            return;
        }

        var remaining = await _dbContext.AttendeeContacts.CountAsync(
            contact => contact.AttendeeId == attendeeId && contact.Id != changing.Id && contact.Kind == generation.RequiredContactKind, cancellationToken);

        if (remaining == 0)
        {
            throw new ConflictException("This is the only contact of the kind required by the generation. Add another one first.");
        }
    }

    private static ContactData Snapshot(AttendeeContact contact) =>
        new() { Kind = contact.Kind, FullName = contact.FullName, Relationship = contact.Relationship, PhoneNumber = contact.PhoneNumber };
}

public class AddContactCommandValidator : AbstractValidator<AddContactCommand>
{
    public AddContactCommandValidator()
    {
        RuleFor(command => command.Contact).NotNull().SetValidator(new ContactDataValidator());
    }
}

public class UpdateContactCommandValidator : AbstractValidator<UpdateContactCommand>
{
    public UpdateContactCommandValidator()
    {
        RuleFor(command => command.Contact).NotNull().SetValidator(new ContactDataValidator());
    }
}

/// <summary>§10: solo "En curso" y "Finalizado"; la fecha solo acompaña a un curso finalizado.</summary>
public class AddCourseCommandValidator : AbstractValidator<AddCourseCommand>
{
    public AddCourseCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(command => command.CourseId).NotEmpty();
        RuleFor(command => command.Status).IsInEnum().WithMessage("Status must be InProgress or Finished");
        RuleFor(command => command.FinishedOn).Null().When(command => command.Status != CourseStatus.Finished)
            .WithMessage("FinishedOn only applies to finished courses");
        RuleFor(command => command.FinishedOn)
            .LessThanOrEqualTo(_ => LocalCalendar.Today(timeProvider)).WithMessage("FinishedOn cannot be in the future");
    }
}

public class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
{
    public UpdateCourseCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(command => command.Status).IsInEnum().WithMessage("Status must be InProgress or Finished");
        RuleFor(command => command.FinishedOn).Null().When(command => command.Status != CourseStatus.Finished)
            .WithMessage("FinishedOn only applies to finished courses");
        RuleFor(command => command.FinishedOn)
            .LessThanOrEqualTo(_ => LocalCalendar.Today(timeProvider)).WithMessage("FinishedOn cannot be in the future");
    }
}
