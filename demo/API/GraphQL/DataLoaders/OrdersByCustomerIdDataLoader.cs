#region

using demo.Domain.Entities;
using demo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

#endregion

namespace demo.API.GraphQL.DataLoaders;

public class OrdersByCustomerIdDataLoader(IServiceProvider services,
    IBatchScheduler batchScheduler,
    DataLoaderOptions options)
    : BatchDataLoader<int, Order[]>(batchScheduler: batchScheduler, options: options)
{
    protected override async Task<IReadOnlyDictionary<int, Order[]>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var orders = await context.Orders
            .Where(o => keys.Contains(o.CustomerId))
            .ToListAsync(cancellationToken);

        return orders
            .GroupBy(o => o.CustomerId)
            .ToDictionary(keySelector: g => g.Key, elementSelector: g => g.ToArray());
    }
}