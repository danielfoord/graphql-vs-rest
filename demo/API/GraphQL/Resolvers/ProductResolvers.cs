#region

using demo.Domain.Entities;
using demo.Infrastructure.Data;

#endregion

namespace demo.API.GraphQL.Resolvers;

/// <summary>
///     Contains resolver methods for the fields of the Product type.
///     This separates the data-fetching logic from the core GraphQL schema definition.
/// </summary>
[ExtendObjectType(typeof(Product))]
public class ProductResolvers
{
    /// <summary>
    ///     Resolver for the "orderLineItems" field on the Product type.
    ///     This method will only be executed if a client query asks for this specific field.
    ///     The [Parent] attribute injects the Product object that was resolved one level up.
    /// </summary>
    /// <param name="product">The parent Product object.</param>
    /// <param name="db">The AppDbContext, injected by DI.</param>
    /// <returns>An IQueryable of the product's line items.</returns>
    [IsProjected(false)]
    public IQueryable<OrderLineItem> GetOrderLineItems(
        [Parent] Product product,
        AppDbContext db)
    {
        return db.OrderLineItems.Where(li => li.ProductId == product.Id);
    }
}