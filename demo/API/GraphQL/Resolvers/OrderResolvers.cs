#region

using demo.API.GraphQL.DataLoaders;
using demo.Domain.Entities;

#endregion

namespace demo.API.GraphQL.Resolvers;

[ExtendObjectType(typeof(Order))]
public class OrderResolvers
{
    [IsProjected(false)]
    public async Task<Customer?> GetCustomer(
        [Parent] Order order,
        CustomerByIdDataLoader customerDataLoader) =>
        await customerDataLoader.LoadAsync(order.CustomerId);

    [IsProjected(false)]
    public async Task<IEnumerable<OrderLineItem>> GetOrderLineItems(
        [Parent] Order order,
        OrderLineItemsByOrderIdDataLoader lineItemDataLoader) =>
        await lineItemDataLoader.LoadAsync(order.Id) ?? [];
}