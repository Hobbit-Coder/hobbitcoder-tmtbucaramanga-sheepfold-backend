using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;
using ValidationException = HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Commands;

/// <summary>
/// El director crea un grupo de un año y una generación. La renovación anual es crear los grupos del
/// año nuevo con este mismo comando; no hay un proceso de cierre aparte.
/// </summary>
public class CreateGroupCommand : IRequest<Guid>
{
    public required int Year { get; set; }
    public required Guid GenerationId { get; set; }
    public required string Name { get; set; }
}

public class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;

    public CreateGroupCommandHandler(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
    }

    public async Task<Guid> Handle(CreateGroupCommand request, CancellationToken cancellationToken = default)
    {
        _access.EnsureDirector();

        var generation = await _dbContext.Generations.FirstOrDefaultAsync(item => item.Id == request.GenerationId && item.IsActive, cancellationToken)
            ?? throw ValidationException.For(nameof(request.GenerationId), "Select an active generation.");

        var name = request.Name.Trim();
        await GroupCommandHelpers.EnsureUniqueAsync(_dbContext, request.Year, generation.Id, name, exceptId: null, cancellationToken);

        var group = new Group { Year = request.Year, GenerationId = generation.Id, Name = name, IsActive = true };
        _dbContext.Groups.Add(group);
        _audit.Record(AuditModule.Groups, AuditActions.Created, group.Id, GroupCommandHelpers.Describe(group, generation), null, new { group.Year, Generation = generation.Name, group.Name, group.IsActive });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return group.Id;
    }
}

public class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator()
    {
        RuleFor(command => command.Year).InclusiveBetween(2000, 2200);
        RuleFor(command => command.GenerationId).NotEmpty().WithMessage("GenerationId is required");
        RuleFor(command => command.Name).NotEmpty().WithMessage("Name is required").MaximumLength(60);
    }
}
