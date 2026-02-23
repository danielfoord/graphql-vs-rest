#region

using demo.Domain.Entities;
using demo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

#endregion

// For Task

namespace demo.API.GraphQL;

/// <summary>
///     Defines the root "Subscription" type for the GraphQL schema.
///     Subscriptions allow clients to listen for real-time events from the server.
///     Each method defines a subscribable event stream.
/// </summary>
public class Subscription
{
    /// <summary>
    ///     A subscription resolver for when a new product is added.
    ///     The [Topic] attribute links this resolver to a specific event stream name,
    ///     which is the same name used by the ITopicEventSender in the Mutation.
    ///     The [Subscribe] attribute marks this as a subscription entry point.
    /// </summary>
    /// <param name="id">The product ID from the event message, sent by the mutation.</param>
    /// <param name="db">The AppDbContext, injected by DI.</param>
    /// <returns>The product that was added.</returns>
    [Subscribe]
    [Topic]
    public async Task<Product?> OnProductAdded([EventMessage] int id, AppDbContext db) =>
        await db.Products.FirstOrDefaultAsync(p => p.Id == id);

    /// <summary>
    ///     A subscription resolver for when a new customer is added.
    /// </summary>
    [Subscribe]
    [Topic]
    public async Task<Customer?> OnCustomerAdded([EventMessage] int id, AppDbContext db) =>
        await db.Customers.FirstOrDefaultAsync(c => c.Id == id);

    /// <summary>
    ///     A subscription resolver for when a new order is added.
    /// </summary>
    [Subscribe]
    [Topic]
    public async Task<Order?> OnOrderAdded([EventMessage] int id, AppDbContext db) =>
        await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
}