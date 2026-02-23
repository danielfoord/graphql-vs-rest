#region

using demo.Domain.Entities;
using demo.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

#endregion

namespace demo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Customer>>> Get([FromQuery] int? productId = null)
    {
        var query = db.Customers.AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(c => c.Orders.Any(o => o.OrderLineItems.Any(op => op.ProductId == productId.Value)));
        }

        return await query.Include(c => c.Orders).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Customer>> Get(int id)
    {
        var customer = await db.Customers.Include(c => c.Orders).FirstOrDefaultAsync(c => c.Id == id);
        return customer is {} c ? c : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> Create(Customer customer)
    {
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return CreatedAtAction(actionName: nameof(Get), routeValues: new
        {
            id = customer.Id
        }, value: customer);
    }
}