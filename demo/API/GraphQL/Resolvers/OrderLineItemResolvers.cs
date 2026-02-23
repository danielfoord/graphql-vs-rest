#region

using demo.API.GraphQL.DataLoaders;
using demo.Domain.Entities;

#endregion

namespace demo.API.GraphQL.Resolvers;

[ExtendObjectType(typeof(OrderLineItem))]
public class OrderLineItemResolvers
{
    [IsProjected(false)]
    public async Task<Product?> GetProduct(
        [Parent] OrderLineItem orderLineItem,
        ProductByIdDataLoader productDataLoader) =>
        await productDataLoader.LoadAsync(orderLineItem.ProductId);
}