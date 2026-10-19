using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Commands;

/// <summary>Edita los datos de la ficha dentro del alcance: director, coordinador, líder y aprendiz.</summary>
public class UpdateAttendeeCommand : IRequest, IAttendeeData
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public Gender? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Instagram { get; set; }
    public DateOnly? BirthDate { get; set; }
    public EducationalStage? EducationalStage { get; set; }
    public bool IsServer { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateAttendeeCommandHandler : IRequestHandler<UpdateAttendeeCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;

    public UpdateAttendeeCommandHandler(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
    }

    public async Task<Unit> Handle(UpdateAttendeeCommand request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.Id, cancellationToken);

        var before = AttendeeCommandHelpers.Snapshot(attendee);
        AttendeeCommandHelpers.Apply(attendee, request);
        attendee.IsActive = request.IsActive;

        _audit.Record(AuditModule.Attendees, AuditActions.ForActivation(before.IsActive, attendee.IsActive), attendee.Id, attendee.FullName, before, AttendeeCommandHelpers.Snapshot(attendee));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public class UpdateAttendeeCommandValidator : AbstractValidator<UpdateAttendeeCommand>
{
    public UpdateAttendeeCommandValidator(TimeProvider timeProvider)
    {
        Include(new AttendeeDataValidator(timeProvider));
    }
}
