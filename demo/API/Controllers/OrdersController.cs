#region

using demo.Domain.Entities;
using demo.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

#endregion

namespace demo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> Get([FromQuery] int? customerId = null)
    {
        var query = db.Orders.AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(o => o.CustomerId == customerId.Value);
        }

        return await query
            .Include(o => o.Customer)
            .Include(o => o.OrderLineItems)
            .ThenInclude(op => op.Product)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> Get(int id)
    {
        var order = await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderLineItems)
            .ThenInclude(op => op.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        return order is {} o ? o : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<Order>> Create(OrderPostModel model)
    {
        var customer = await db.Customers.FindAsync(model.CustomerId);
        if (customer == null)
        {
            return NotFound("Customer not found");
        }

        var order = new Order
        {
            CustomerId = model.CustomerId,
            OrderDate = DateTime.UtcNow,
            OrderLineItems = model.Items.Select(i => new OrderLineItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return CreatedAtAction(actionName: nameof(Get), routeValues: new
        {
            id = order.Id
        }, value: order);
    }

    public record OrderPostModel(int CustomerId, List<OrderLineItemPostModel> Items);

    public record OrderLineItemPostModel(int ProductId, int Quantity);
}