using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Commands;

/// <summary>
/// Edita el contenido de un realizado o su confidencialidad: el autor o el director. Retirar la
/// confidencialidad pasa el contenido a la consulta jerárquica ordinaria y queda auditado.
/// </summary>
public class UpdateFollowUpContentCommand : IRequest
{
    public Guid Id { get; set; }
    public required FollowUpContent Content { get; set; }
}

public class UpdateFollowUpContentCommandHandler : IRequestHandler<UpdateFollowUpContentCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;
    private readonly AuditTrail _audit;

    public UpdateFollowUpContentCommandHandler(
        IApplicationDbContext dbContext,
        MinistryAccess access,
        IUserDirectory userDirectory,
        AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
        _audit = audit;
    }

    public async Task<Unit> Handle(UpdateFollowUpContentCommand request, CancellationToken cancellationToken = default)
    {
        var followUp = await FollowUpCommandHelpers.FindEditableAsync(_dbContext, _access, _userDirectory, request.Id, cancellationToken);

        if (followUp.Status != FollowUpStatus.Done)
        {
            throw new ConflictException("Complete the follow-up before editing its content.");
        }

        var before = FollowUpCommandHelpers.Snapshot(followUp);
        var wasConfidential = followUp.IsConfidential;
        await FollowUpCommandHelpers.ApplyContentAsync(_dbContext, followUp, request.Content, cancellationToken);

        var action = wasConfidential == followUp.IsConfidential ? AuditActions.Updated : AuditActions.ConfidentialityChanged;
        _audit.Record(AuditModule.FollowUps, action, followUp.Id, "Seguimiento", before, FollowUpCommandHelpers.Snapshot(followUp));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public class UpdateFollowUpContentCommandValidator : AbstractValidator<UpdateFollowUpContentCommand>
{
    public UpdateFollowUpContentCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(command => command.Content).NotNull().SetValidator(new FollowUpContentValidator(timeProvider));
    }
}
