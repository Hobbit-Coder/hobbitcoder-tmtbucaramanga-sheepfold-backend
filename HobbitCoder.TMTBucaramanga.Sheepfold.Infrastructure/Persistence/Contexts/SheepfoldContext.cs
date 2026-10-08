using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Contexts;

public class SheepfoldContext : DbContext, IApplicationDbContext
{
    private const int AuditUserMaxLength = 256;

    public DbSet<TodoItem> TodoItems { get; set; }
    public DbSet<TodoList> TodoLists { get; set; }

    public SheepfoldContext(DbContextOptions<SheepfoldContext> options) : base(options)
    {
        TodoItems = Set<TodoItem>();
        TodoLists = Set<TodoList>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PersistenceConstants.Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SheepfoldContext).Assembly);

        ApplyAuditableConventions(modelBuilder);
        ApplySoftDeleteQueryFilters(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void ApplyAuditableConventions(ModelBuilder modelBuilder)
    {
        foreach (var clrType in GetEntityClrTypes<IAuditableEntity>(modelBuilder))
        {
            var entity = modelBuilder.Entity(clrType);

            entity.Property(nameof(IAuditableEntity.CreatedBy)).HasMaxLength(AuditUserMaxLength);
            entity.Property(nameof(IAuditableEntity.UpdatedBy)).HasMaxLength(AuditUserMaxLength);

            if (typeof(ISoftDeleteableEntity).IsAssignableFrom(clrType))
            {
                entity.Property(nameof(ISoftDeleteableEntity.DeletedBy)).HasMaxLength(AuditUserMaxLength);
            }
        }
    }

    /// <summary>
    /// Oculta los registros borrados lógicamente en todas las consultas. Para verlos,
    /// una consulta debe pedirlo explícitamente con <c>IgnoreQueryFilters()</c>.
    /// </summary>
    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var clrType in GetEntityClrTypes<ISoftDeleteableEntity>(modelBuilder))
        {
            var entity = Expression.Parameter(clrType, "entity");
            var isNotDeleted = Expression.Not(Expression.Property(entity, nameof(ISoftDeleteableEntity.IsDeleted)));

            modelBuilder.Entity(clrType).HasQueryFilter(Expression.Lambda(isNotDeleted, entity));
        }
    }

    private static List<Type> GetEntityClrTypes<TContract>(ModelBuilder modelBuilder) =>
        modelBuilder.Model.GetEntityTypes()
            .Select(entityType => entityType.ClrType)
            .Where(clrType => typeof(TContract).IsAssignableFrom(clrType))
            .ToList();
}
