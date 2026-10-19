using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Commands;

/// <summary>
/// Cambia el nombre o el estado del grupo. Inactivarlo exige que ningún asistente lo tenga como grupo vigente
///: primero se trasladan. El año y la generación no cambian: alterarían la historia de sus asistentes.
/// </summary>
public class UpdateGroupCommand : IRequest
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateGroupCommandHandler : IRequestHandler<UpdateGroupCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;

    public UpdateGroupCommandHandler(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
    }

    public async Task<Unit> Handle(UpdateGroupCommand request, CancellationToken cancellationToken = default)
    {
        var group = await GroupCommandHelpers.FindEditableAsync(_dbContext, _access, request.Id, cancellationToken);
        var name = request.Name.Trim();
        await GroupCommandHelpers.EnsureUniqueAsync(_dbContext, group.Year, group.GenerationId, name, group.Id, cancellationToken);

        if (group.IsActive && !request.IsActive)
        {
            await GroupCommandHelpers.EnsureNoCurrentMembersAsync(_dbContext, group.Id, cancellationToken);
        }

        var before = new { group.Name, group.IsActive };
        group.Name = name;
        group.IsActive = request.IsActive;

        var action = AuditActions.ForActivation(before.IsActive, group.IsActive);
        _audit.Record(AuditModule.Groups, action, group.Id, GroupCommandHelpers.Describe(group, group.Generation), before, new { group.Name, group.IsActive });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public class UpdateGroupCommandValidator : AbstractValidator<UpdateGroupCommand>
{
    public UpdateGroupCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().WithMessage("Name is required").MaximumLength(60);
    }
}
