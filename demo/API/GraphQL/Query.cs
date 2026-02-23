#region

using demo.Domain.Entities;
using demo.Infrastructure.Data;

#endregion

namespace demo.API.GraphQL;

/// <summary>
///     Defines the root "Query" type for the GraphQL schema.
///     This class contains all the top-level fields that clients can query to fetch data.
///     In HotChocolate, this is the entry point for all data retrieval operations.
/// </summary>
public class Query
{
    [UseProjection]
    [UseFiltering]
    public IQueryable<Product> GetProducts(AppDbContext db) => db.Products;

    public Product? GetProduct(int id, AppDbContext db) =>
        db.Products.FirstOrDefault(p => p.Id == id);

    [UseProjection]
    [UseFiltering]
    public IQueryable<Customer> GetCustomers(AppDbContext db) => db.Customers;

    public Customer? GetCustomer(int id, AppDbContext db) =>
        db.Customers.FirstOrDefault(c => c.Id == id);

    [UseProjection]
    [UseFiltering]
    public IQueryable<Order> GetOrders(AppDbContext db) => db.Orders;

    public Order? GetOrder(int id, AppDbContext db) => db.Orders.FirstOrDefault(o => o.Id == id);
}