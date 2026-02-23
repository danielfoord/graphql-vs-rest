#region

using demo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

#endregion

namespace demo.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<OrderLineItem> OrderLineItems => Set<OrderLineItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // One-to-Many: Customer to Order
        modelBuilder.Entity<Customer>()
            .HasMany(c => c.Orders)
            .WithOne(o => o.Customer)
            .HasForeignKey(o => o.CustomerId);

        // Many-to-Many: Order to Product through OrderLineItem
        modelBuilder.Entity<OrderLineItem>()
            .HasOne(op => op.Order)
            .WithMany(o => o.OrderLineItems)
            .HasForeignKey(op => op.OrderId);

        modelBuilder.Entity<OrderLineItem>()
            .HasOne(op => op.Product)
            .WithMany(p => p.OrderLineItems)
            .HasForeignKey(op => op.ProductId);

        // Precision for Price
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(precision: 18, scale: 2);
    }
}