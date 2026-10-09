using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings;

/// <summary>Listas administrables (§27, HU-29).</summary>
public enum CatalogKind
{
    Generations = 1,
    EventTypes = 2,
    FollowUpTopics = 3,
    Courses = 4
}

/// <summary>Nombre del catálogo en la auditoría, en español como el resto de la interfaz.</summary>
public static class CatalogKindLabels
{
    public static string Of(CatalogKind kind) => kind switch
    {
        CatalogKind.Generations => "Generación",
        CatalogKind.EventTypes => "Tipo de evento",
        CatalogKind.FollowUpTopics => "Tema de seguimiento",
        CatalogKind.Courses => "Curso",
        _ => kind.ToString()
    };
}

public sealed record CatalogItemResponse(Guid Id, string Name, bool IsActive, GenerationProfile? Profile = null, int? SortOrder = null);

public sealed record CatalogsResponse(
    IReadOnlyList<CatalogItemResponse> Generations,
    IReadOnlyList<CatalogItemResponse> EventTypes,
    IReadOnlyList<CatalogItemResponse> FollowUpTopics,
    IReadOnlyList<CatalogItemResponse> Courses);

/// <summary>
/// Todas las listas. Las consultan el administrador (para administrarlas) y los usuarios ministeriales (para
/// operar); no contienen información ministerial. Por defecto solo los valores activos.
/// </summary>
public class GetCatalogsQuery : IRequest<CatalogsResponse>
{
    public bool IncludeInactive { get; set; }
}

/// <summary>El administrador registra un valor (§27). Las generaciones llevan su perfil de la fuente.</summary>
public class CreateCatalogItemCommand : IRequest<Guid>
{
    public CatalogKind Kind { get; set; }
    public required string Name { get; set; }
    public GenerationProfile? Profile { get; set; }
    public int? SortOrder { get; set; }
}

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

public class CatalogHandlers :
    IRequestHandler<GetCatalogsQuery, CatalogsResponse>,
    IRequestHandler<CreateCatalogItemCommand, Guid>,
    IRequestHandler<UpdateCatalogItemCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;

    public CatalogHandlers(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
    }

    public async Task<CatalogsResponse> Handle(GetCatalogsQuery request, CancellationToken cancellationToken = default)
    {
        if (!_access.IsAdmin && !_access.IsMinistry)
        {
            throw new ForbiddenAccessException();
        }

        // Solo el administrador ve los valores retirados (para restaurarlos).
        var includeInactive = request.IncludeInactive && _access.IsAdmin;

        var generations = await _dbContext.Generations
            .Where(item => includeInactive || item.IsActive)
            .OrderBy(item => item.SortOrder).ThenBy(item => item.Name)
            .Select(item => new CatalogItemResponse(item.Id, item.Name, item.IsActive, item.Profile, item.SortOrder))
            .ToListAsync(cancellationToken);

        return new CatalogsResponse(
            generations,
            await ListAsync(_dbContext.EventTypes, includeInactive, cancellationToken),
            await ListAsync(_dbContext.FollowUpTopics, includeInactive, cancellationToken),
            await ListAsync(_dbContext.Courses, includeInactive, cancellationToken));
    }

    public async Task<Guid> Handle(CreateCatalogItemCommand request, CancellationToken cancellationToken = default)
    {
        _access.EnsureAdmin();
        var name = request.Name.Trim();

        CatalogItem item = request.Kind switch
        {
            CatalogKind.Generations => new Generation
            {
                Name = name,
                Profile = request.Profile ?? GenerationProfile.Other,
                SortOrder = request.SortOrder ?? await _dbContext.Generations.CountAsync(cancellationToken) + 1
            },
            CatalogKind.EventTypes => new EventType { Name = name },
            CatalogKind.FollowUpTopics => new FollowUpTopic { Name = name },
            _ => new Course { Name = name }
        };

        if (await NameExistsAsync(request.Kind, name, exceptId: null, cancellationToken))
        {
            throw new ConflictException($"The value '{name}' already exists.");
        }

        AddItem(item);
        _audit.Record(AuditModule.Configuration, AuditActions.Created, item.Id, $"{CatalogKindLabels.Of(request.Kind)}: {name}", null, Snapshot(item));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return item.Id;
    }

    public async Task<Unit> Handle(UpdateCatalogItemCommand request, CancellationToken cancellationToken = default)
    {
        _access.EnsureAdmin();

        var item = await FindAsync(request.Kind, request.Id, cancellationToken);
        var name = request.Name.Trim();

        if (await NameExistsAsync(request.Kind, name, item.Id, cancellationToken))
        {
            throw new ConflictException($"The value '{name}' already exists.");
        }

        var before = Snapshot(item);
        item.Name = name;
        item.IsActive = request.IsActive;

        if (item is Generation generation && request.SortOrder is { } sortOrder)
        {
            generation.SortOrder = sortOrder;
        }

        var action = AuditActions.ForActivation(before.IsActive, item.IsActive, AuditActions.Restored, AuditActions.Retired);
        _audit.Record(AuditModule.Configuration, action, item.Id, $"{CatalogKindLabels.Of(request.Kind)}: {name}", before, Snapshot(item));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private static async Task<IReadOnlyList<CatalogItemResponse>> ListAsync<T>(IQueryable<T> items, bool includeInactive, CancellationToken cancellationToken)
        where T : CatalogItem =>
        await items
            .Where(item => includeInactive || item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new CatalogItemResponse(item.Id, item.Name, item.IsActive, null, null))
            .ToListAsync(cancellationToken);

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

    private Task<bool> NameExistsAsync(CatalogKind kind, string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var normalized = name.ToUpperInvariant();

        return kind switch
        {
            CatalogKind.Generations => _dbContext.Generations.AnyAsync(item => item.Id != exceptId && item.Name.ToUpper() == normalized, cancellationToken),
            CatalogKind.EventTypes => _dbContext.EventTypes.AnyAsync(item => item.Id != exceptId && item.Name.ToUpper() == normalized, cancellationToken),
            CatalogKind.FollowUpTopics => _dbContext.FollowUpTopics.AnyAsync(item => item.Id != exceptId && item.Name.ToUpper() == normalized, cancellationToken),
            _ => _dbContext.Courses.AnyAsync(item => item.Id != exceptId && item.Name.ToUpper() == normalized, cancellationToken)
        };
    }

    private void AddItem(CatalogItem item)
    {
        switch (item)
        {
            case Generation generation: _dbContext.Generations.Add(generation); break;
            case EventType eventType: _dbContext.EventTypes.Add(eventType); break;
            case FollowUpTopic topic: _dbContext.FollowUpTopics.Add(topic); break;
            case Course course: _dbContext.Courses.Add(course); break;
        }
    }

    private static CatalogSnapshot Snapshot(CatalogItem item) =>
        new(item.Name, item.IsActive, (item as Generation)?.Profile, (item as Generation)?.SortOrder);

    private sealed record CatalogSnapshot(string Name, bool IsActive, GenerationProfile? Profile, int? SortOrder);
}

public class CreateCatalogItemCommandValidator : AbstractValidator<CreateCatalogItemCommand>
{
    public CreateCatalogItemCommandValidator()
    {
        RuleFor(command => command.Kind).IsInEnum();
        RuleFor(command => command.Name).NotEmpty().WithMessage("Name is required").MaximumLength(100);
        RuleFor(command => command.Profile).IsInEnum();
        RuleFor(command => command.Profile).Null().When(command => command.Kind != CatalogKind.Generations)
            .WithMessage("Profile only applies to generations");
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
