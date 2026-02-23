#region

using demo.Domain.Entities;
using demo.Infrastructure.Data;
using HotChocolate.Subscriptions;

#endregion

namespace demo.API.GraphQL;

// These records are Data Transfer Objects (DTOs) specifically for our mutations.
// They define the shape of the data the client needs to send to perform an action.
// Using dedicated input types is a GraphQL best practice.
public record AddProductInput(string Name, decimal Price);

public record AddCustomerInput(string Name, string Email);

public record AddOrderLineItemInput(int ProductId, int Quantity);

public record AddOrderInput(int CustomerId, List<AddOrderLineItemInput> Items);

/// <summary>
///     Defines the root "Mutation" type for the GraphQL schema.
///     This class contains all the top-level fields that clients can use to modify data.
///     Each method corresponds to a data-changing operation.
/// </summary>
public class Mutation
{
    /// <summary>
    ///     Creates a new product and publishes a subscription event.
    /// </summary>
    /// <param name="input">The product data from the client.</param>
    /// <param name="db">The AppDbContext for database operations.</param>
    /// <param name="eventSender">The subscription event bus to publish messages.</param>
    /// <returns>The newly created product.</returns>
    public async Task<Product> AddProductAsync(
        AddProductInput input,
        AppDbContext db,
        [Service] ITopicEventSender eventSender)
    {
        var product = new Product
        {
            Name = input.Name,
            Price = input.Price
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        // For subscriptions, it's most efficient to send only the ID of the changed entity.
        // The subscription resolver is then responsible for fetching the data,
        // which prevents over-fetching data that no subscriber requested.
        await eventSender.SendAsync(topicName: nameof(Subscription.OnProductAdded), message: product.Id);

        return product;
    }

    /// <summary>
    ///     Creates a new customer and publishes a subscription event.
    /// </summary>
    public async Task<Customer> AddCustomerAsync(
        AddCustomerInput input,
        AppDbContext db,
        [Service] ITopicEventSender eventSender)
    {
        var customer = new Customer
        {
            Name = input.Name,
            Email = input.Email
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        await eventSender.SendAsync(topicName: nameof(Subscription.OnCustomerAdded), message: customer.Id);

        return customer;
    }

    /// <summary>
    ///     Creates a new order and publishes a subscription event.
    /// </summary>
    public async Task<Order> AddOrderAsync(
        AddOrderInput input,
        AppDbContext db,
        [Service] ITopicEventSender eventSender)
    {
        var customer = await db.Customers.FindAsync(input.CustomerId);
        if (customer is null)
        {
            throw new GraphQLException("Customer not found");
        }

        var order = new Order
        {
            CustomerId = input.CustomerId,
            OrderDate = DateTime.UtcNow,
            OrderLineItems = input.Items.Select(i => new OrderLineItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        await eventSender.SendAsync(topicName: nameof(Subscription.OnOrderAdded), message: order.Id);

        // The returned Order object will be handled by HotChocolate.
        // If the client's selection set includes relationships (like Customer or LineItems),
        // the dedicated resolvers will be invoked to fetch that data. We don't need to
        // manually load or include it here.
        return order;
    }
}