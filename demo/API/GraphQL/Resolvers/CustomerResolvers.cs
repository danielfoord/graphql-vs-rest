#region

using demo.API.GraphQL.DataLoaders;
using demo.Domain.Entities;

#endregion

namespace demo.API.GraphQL.Resolvers;

[ExtendObjectType(typeof(Customer))]
public class CustomerResolvers
{
    [IsProjected(false)]
    public async Task<IEnumerable<Order>> GetOrders(
        [Parent] Customer customer,
        OrdersByCustomerIdDataLoader orderDataLoader) =>
        await orderDataLoader.LoadAsync(customer.Id) ?? [];
}