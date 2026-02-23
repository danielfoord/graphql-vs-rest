---
marp: true
paginate: true
headingDivider: 2
title: ".NET Core APIs — REST vs GraphQL"
description: "A deep dive into REST APIs and GraphQL APIs with HotChocolate in .NET"
theme: default
html: true
class:
  - lead
  - invert
---

# .NET Core APIs — REST vs GraphQL

<script type="module">
  import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';
  mermaid.initialize({ startOnLoad: true });
</script>

### A technical deep dive

## Agenda

1. What is a Web API?
2. REST fundamentals
3. GraphQL fundamentals
4. Mutations and Subscriptions
5. Performance: Projections & DataLoaders
6. Aggregating APIs (Gateway vs Stitching)
7. Summary & Conclusion

## What is a Web API?

- An API (Application Programming Interface) exposes data and operations to
  clients.
- In .NET Core, Web APIs typically use controllers and routes.
- Two major approaches:
  - REST — resource-based
  - GraphQL — query-based

## REST in .NET Core

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> Get() => 
        await db.Products.ToListAsync();

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> Get(int id)
    {
        var product = await db.Products.FindAsync(id);
        return product is { } p ? p : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Create(Product product)
    {
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }
}
```

## REST Principles

- Resources identified by URLs
- CRUD operations via HTTP verbs (GET, POST, PUT, DELETE)
- Stateless — each request is independent
- Structured responses (JSON, XML)
- Example: `/api/products/5`

## REST Pros and Cons

**Pros:**

- Simple, mature, and widely supported
- Works seamlessly with HTTP and caching
- Great for fixed data shapes

**Cons:**

- **Over-fetching / under-fetching problems**
- **Multiple round-trips** for related data
- Versioning complexity

## Enter GraphQL

- Developed by Facebook to solve REST limitations
- Single endpoint: `/graphql`
- Client defines exact data shape
- Strongly typed schema
- Supports queries, mutations, and subscriptions

## Fetching Related Data: REST vs GraphQL

### REST: Resource-based filtering
- `GET /api/orders`
- `GET /api/orders/1`
- `GET /api/orders?customerId=5`
- `GET /api/customers?productId=10`
- **Requires custom controller logic** for every relationship.

## GraphQL: Relationship-based nesting
```graphql
query {
  customer(id: 5) {
    name
    orders {
      id
      orderDate
      lineItems {
        product { name }
      }
    }
  }
}
```

## HotChocolate in .NET

- Open-source GraphQL server for .NET
- Easy integration with ASP.NET Core
- Schema-first or code-first
- Advanced features: filtering, sorting, projections, subscriptions

## GraphQL in .NET Core (HotChocolate)

```csharp
public class Query(AppDbContext db)
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Product> GetProducts() => db.Products;

    [UseProjection]
    [UseFirstOrDefault]
    public IQueryable<Product> GetProduct(int id) => 
        db.Products.Where(p => p.Id == id);
}

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddDbContextFactory<AppDbContext>(...)
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();
```

## GraphQL Schema Example

```graphql
type Product {
  id: ID!
  name: String!
  price: Float!
}

type Customer {
  id: ID!
  name: String!
  email: String!
  orders: [Order!]!
}

type Order {
  id: ID!
  orderDate: String!
  lineItems: [OrderLineItem!]!
}

type Query {
  products: [Product!]!
  customers: [Customer!]!
}
```

## GraphQL Mutations

Mutations allow data modification.

```graphql
mutation {
  addProduct(input: { name: "Keyboard", price: 49.99 }) {
    id
    name
  }
}
```

## Mutations in HotChocolate

```csharp
public record AddProductInput(string Name, decimal Price);

public class Mutation(AppDbContext db)
{
    public async Task<Product> AddProduct(AddProductInput input)
    {
        var product = new Product { Name = input.Name, Price = input.Price };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }
}
```

## GraphQL Subscriptions

- Real-time updates over WebSocket
- Clients subscribe to events and receive data automatically
- Perfect for live dashboards or notifications

## Subscriptions in HotChocolate

```csharp
public class Subscription
{
    [Subscribe]
    [Topic]
    public async Task<Product?> OnProductAdded([EventMessage] int id, AppDbContext db) => 
        await db.Products.FirstOrDefaultAsync(p => p.Id == id);
}

// In Program.cs
builder.Services.AddGraphQLServer()
    ...
    .AddSubscriptionType<Subscription>()
    .AddInMemorySubscriptions();

app.UseWebSockets();
```

## Performance: Projections

- GraphQL lets the client specify which fields to return.
- HotChocolate supports automatic projection to EF Core.
- Prevents over-fetching and optimizes database queries.

```graphql
query {
  customers {
    name
    orders { orderDate }
  }
}
```

HotChocolate translates this into a SQL query selecting **only** the necessary
columns and related data via JOINs.

## Performance: DataLoaders

### The N+1 Problem
If you have a resolver for a child property, it might execute one query for every parent record.

### The Solution: Batching
DataLoaders collect IDs and fetch them in a single batch.

```csharp
public class ProductByIdDataLoader : BatchDataLoader<int, Product>
{
    protected override async Task<IReadOnlyDictionary<int, Product>> LoadBatchAsync(...)
    {
        return await db.Products.Where(p => keys.Contains(p.Id)).ToDictionaryAsync(...);
    }
}
```

## Projections vs. DataLoaders

### When to use Projections (`[UseProjection]`)
- **Same Database:** EF Core can efficiently generate a `JOIN` or subquery.
- **Single Round-trip:** Fetching shallow relationships is faster in one query.
- **Simplicity:** Automatic "stitching" based on the query.

### When to use DataLoaders
- **Cross-Boundary:** Fetching from different sources (e.g., SQL + REST API).
- **Custom Logic:** When the resolver logic cannot be translated to SQL.
- **Request Caching:** Prevents fetching the same entity twice in one request.

## Aggregating APIs: REST vs GraphQL

- Multi-service architectures often require clients to fetch data from multiple backends.
- Two common approaches: **API Gateway** (REST) vs **Schema Stitching** (GraphQL).

---

### REST: API Gateway
<div class="mermaid">
  graph LR
    A[Client] -->|HTTP| B[API Gateway]
    B -->|Route| C[Service 1]
    B -->|Route| D[Service 2]
</div>

### GraphQL: Schema Stitching
<div class="mermaid">
    graph LR
    F[Client] -->|Query| G[Unified Schema Gateway]
    G -->|Delegate| H[Service 1]
    G -->|Delegate| I[Service 2]
</div>

## Aggregation Comparison

**REST → API Gateway**
- Single entry point routing requests.
- Handles auth, caching, rate-limiting.
- Fixed data shapes; over-/under-fetching still possible.

**GraphQL → Schema Stitching / Federation**
- Combines multiple schemas into one unified API.
- Clients query multiple services in **one single request**.
- Avoids multiple round-trips and over-fetching.

## REST vs GraphQL Summary

| Feature    | REST                | GraphQL          |
| ---------- | ------------------- | ---------------- |
| Endpoint   | Multiple            | Single           |
| Data Shape | Fixed               | Client-defined   |
| Versioning | URL-based           | Schema evolution |
| Real-time  | Webhooks / Polling  | Subscriptions    |
| Efficiency | Over/Under fetching | Exact fetching   |

## When to Use Which?

**REST**
- Simple, stable APIs.
- Public APIs requiring standard HTTP caching.
- Standard microservice-to-microservice communication.

**GraphQL**
- Complex data relationships & deep nesting.
- Multiple frontends with different data needs.
- Real-time requirement (Subscriptions).
- Aggregating multiple backends into one query.

## Conclusion: Key Takeaways

- **REST** is mature and simple, but suffers from data fetching inefficiencies in complex UIs.
- **GraphQL** empowers clients to fetch exactly what they need in one call.
- **HotChocolate** brings enterprise-grade GraphQL features to .NET with minimal boilerplate.
- **Projections** optimize database calls, while **DataLoaders** solve the N+1 problem and enable cross-service data fetching.

# Thank You

### Questions?
