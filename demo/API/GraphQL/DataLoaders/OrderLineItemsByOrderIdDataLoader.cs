#region

using demo.Domain.Entities;
using demo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

#endregion

namespace demo.API.GraphQL.DataLoaders;

public class OrderLineItemsByOrderIdDataLoader(IServiceProvider services,
    IBatchScheduler batchScheduler,
    DataLoaderOptions options)
    : BatchDataLoader<int, OrderLineItem[]>(batchScheduler: batchScheduler, options: options)
{
    protected override async Task<IReadOnlyDictionary<int, OrderLineItem[]>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var lineItems = await context.OrderLineItems
            .Where(li => keys.Contains(li.OrderId))
            .ToListAsync(cancellationToken);

        return lineItems
            .GroupBy(li => li.OrderId)
            .ToDictionary(keySelector: g => g.Key, elementSelector: g => g.ToArray());
    }
}