#region

using demo.Domain.Entities;

#endregion

namespace demo.Infrastructure.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext db)
    {
        // For development, ensure database is recreated to apply schema changes
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        if (!db.Products.Any())
        {
            var laptop = new Product
            {
                Name = "Laptop",
                Price = 999.99m
            };
            var mouse = new Product
            {
                Name = "Mouse",
                Price = 25.50m
            };
            var keyboard = new Product
            {
                Name = "Keyboard",
                Price = 50.00m
            };

            db.Products.AddRange(laptop, mouse, keyboard);

            var customer = new Customer
            {
                Name = "John Doe",
                Email = "john@example.com"
            };
            db.Customers.Add(customer);

            var order = new Order
            {
                Customer = customer,
                OrderDate = DateTime.UtcNow,
                OrderLineItems = new List<OrderLineItem>
                {
                    new()
                    {
                        Product = laptop,
                        Quantity = 1
                    },
                    new()
                    {
                        Product = mouse,
                        Quantity = 2
                    }
                }
            };

            db.Orders.Add(order);
            db.SaveChanges();
        }
    }
}