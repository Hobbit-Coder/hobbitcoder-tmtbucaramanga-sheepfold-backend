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

/// <summary>Completa el mismo pendiente con la información de la reunión: no crea otro registro.</summary>
public class CompleteFollowUpCommand : IRequest
{
    public Guid Id { get; set; }
    public required FollowUpContent Content { get; set; }
}

public class CompleteFollowUpCommandHandler : IRequestHandler<CompleteFollowUpCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;
    private readonly AuditTrail _audit;

    public CompleteFollowUpCommandHandler(
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

    public async Task<Unit> Handle(CompleteFollowUpCommand request, CancellationToken cancellationToken = default)
    {
        var followUp = await FollowUpCommandHelpers.FindEditableAsync(_dbContext, _access, _userDirectory, request.Id, cancellationToken);

        if (followUp.Status != FollowUpStatus.Pending)
        {
            throw new ConflictException("The follow-up is already done.");
        }

        var before = FollowUpCommandHelpers.Snapshot(followUp);
        await FollowUpCommandHelpers.ApplyContentAsync(_dbContext, followUp, request.Content, cancellationToken);

        _audit.Record(AuditModule.FollowUps, AuditActions.Completed, followUp.Id, "Seguimiento", before, FollowUpCommandHelpers.Snapshot(followUp));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public class CompleteFollowUpCommandValidator : AbstractValidator<CompleteFollowUpCommand>
{
    public CompleteFollowUpCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(command => command.Content).NotNull().SetValidator(new FollowUpContentValidator(timeProvider));
    }
}
