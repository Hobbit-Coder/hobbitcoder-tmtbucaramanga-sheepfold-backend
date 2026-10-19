using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Commands;

/// <summary>
/// El administrador renombra un valor o lo activa/desactiva. El perfil de una generación no cambia después de
/// creada: cambiaría retroactivamente el contacto exigido a sus asistentes.
/// </summary>
public class UpdateCatalogItemCommand : IRequest
{
    public CatalogKind Kind { get; set; }
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
    public int? SortOrder { get; set; }
}

public class UpdateCatalogItemCommandHandler : IRequestHandler<UpdateCatalogItemCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;

    public UpdateCatalogItemCommandHandler(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
    }

    public async Task<Unit> Handle(UpdateCatalogItemCommand request, CancellationToken cancellationToken = default)
    {
        _access.EnsureAdmin();

        var item = await FindAsync(request.Kind, request.Id, cancellationToken);
        var name = request.Name.Trim();

        if (await CatalogHelpers.NameExistsAsync(_dbContext, request.Kind, name, item.Id, cancellationToken))
        {
            throw new ConflictException($"The value '{name}' already exists.");
        }

        var before = CatalogHelpers.Snapshot(item);
        item.Name = name;
        item.IsActive = request.IsActive;

        if (item is Generation generation && request.SortOrder is { } sortOrder)
        {
            generation.SortOrder = sortOrder;
        }

        var action = AuditActions.ForActivation(before.IsActive, item.IsActive, AuditActions.Restored, AuditActions.Retired);
        _audit.Record(AuditModule.Configuration, action, item.Id, $"{CatalogKindLabels.Of(request.Kind)}: {name}", before, CatalogHelpers.Snapshot(item));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private async Task<CatalogItem> FindAsync(CatalogKind kind, Guid id, CancellationToken cancellationToken)
    {
        CatalogItem? item = kind switch
        {
            CatalogKind.Generations => await _dbContext.Generations.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken),
            CatalogKind.EventTypes => await _dbContext.EventTypes.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken),
            CatalogKind.FollowUpTopics => await _dbContext.FollowUpTopics.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken),
            _ => await _dbContext.Courses.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken)
        };

        return item ?? throw new NotFoundException(kind.ToString(), id);
    }
}

public class UpdateCatalogItemCommandValidator : AbstractValidator<UpdateCatalogItemCommand>
{
    public UpdateCatalogItemCommandValidator()
    {
        RuleFor(command => command.Kind).IsInEnum();
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().WithMessage("Name is required").MaximumLength(100);
    }
}
