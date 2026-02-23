#region

using demo.Domain.Entities;
using demo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

#endregion

namespace demo.API.GraphQL.DataLoaders;

public class ProductByIdDataLoader(IServiceProvider services,
    IBatchScheduler batchScheduler,
    DataLoaderOptions options)
    : BatchDataLoader<int, Product>(batchScheduler: batchScheduler, options: options)
{
    protected override async Task<IReadOnlyDictionary<int, Product>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await context.Products
            .Where(p => keys.Contains(p.Id))
            .ToDictionaryAsync(keySelector: p => p.Id, cancellationToken: cancellationToken);
    }
}