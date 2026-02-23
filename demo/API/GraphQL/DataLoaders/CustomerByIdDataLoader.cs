#region

using demo.Domain.Entities;
using demo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

#endregion

namespace demo.API.GraphQL.DataLoaders;

public class CustomerByIdDataLoader(IServiceProvider services,
    IBatchScheduler batchScheduler,
    DataLoaderOptions options)
    : BatchDataLoader<int, Customer>(batchScheduler: batchScheduler, options: options)
{
    protected override async Task<IReadOnlyDictionary<int, Customer>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await context.Customers
            .Where(c => keys.Contains(c.Id))
            .ToDictionaryAsync(keySelector: c => c.Id, cancellationToken: cancellationToken);
    }
}