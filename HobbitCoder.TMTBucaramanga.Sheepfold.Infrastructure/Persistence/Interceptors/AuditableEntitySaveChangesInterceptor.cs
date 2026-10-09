using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Completa los campos de auditoría al guardar y convierte en borrado lógico la eliminación
/// de entidades <see cref="ISoftDeleteableEntity"/>: los handlers solo hacen <c>Remove</c>,
/// sin asignar fechas ni usuarios a mano.
/// </summary>
public class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    public AuditableEntitySaveChangesInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        var currentUser = _currentUserService.UserId;
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = currentUser;
                    entry.Entity.CreatedDate = now;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedBy = currentUser;
                    entry.Entity.UpdatedDate = now;
                    break;

                case EntityState.Deleted when entry.Entity is ISoftDeleteableEntity { IsDeleted: false } softDeleteableEntity:
                    entry.State = EntityState.Modified;
                    softDeleteableEntity.IsDeleted = true;
                    softDeleteableEntity.DeletedAt = now;
                    softDeleteableEntity.DeletedBy = currentUser;
                    break;
            }
        }
    }
}
