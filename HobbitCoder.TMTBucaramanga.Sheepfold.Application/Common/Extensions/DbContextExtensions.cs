using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Extensions;

public static class DbContextExtensions
{
    /// <summary>
    /// Ejecuta <paramref name="operation"/> en una transacción, dentro de la estrategia de reintentos del
    /// proveedor (una transacción manual no se puede reintentar fuera de ella). Si falla, no queda nada guardado.
    /// </summary>
    public static Task InTransactionAsync(this IApplicationDbContext dbContext, Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(operation);

        var strategy = dbContext.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }
}
