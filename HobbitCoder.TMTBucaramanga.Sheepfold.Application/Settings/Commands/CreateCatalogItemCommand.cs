using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Commands;

/// <summary>El administrador registra un valor. Las generaciones llevan su perfil de la fuente.</summary>
public class CreateCatalogItemCommand : IRequest<Guid>
{
    public CatalogKind Kind { get; set; }
    public required string Name { get; set; }
    public GenerationProfile? Profile { get; set; }
    public int? SortOrder { get; set; }
}

public class CreateCatalogItemCommandHandler : IRequestHandler<CreateCatalogItemCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;

    public CreateCatalogItemCommandHandler(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
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

        if (await CatalogHelpers.NameExistsAsync(_dbContext, request.Kind, name, exceptId: null, cancellationToken))
        {
            throw new ConflictException($"The value '{name}' already exists.");
        }

        AddItem(item);
        _audit.Record(AuditModule.Configuration, AuditActions.Created, item.Id, $"{CatalogKindLabels.Of(request.Kind)}: {name}", null, CatalogHelpers.Snapshot(item));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return item.Id;
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
