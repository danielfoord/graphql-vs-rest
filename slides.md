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
4. Comparing REST and GraphQL
5. Demo examples in .NET Core
6. Mutations and Subscriptions
7. Query Projection in GraphQL
8. Q&A

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

- Over-fetching / under-fetching problems
- Multiple round-trips for related data
- Versioning complexity

## Fetching Related Data

### REST: Resource-based filtering
- `GET /api/orders?customerId=5`
- `GET /api/customers?productId=10`
- **Requires custom controller logic** for every filter/relationship.

## GraphQL: Relationship-based nesting
```graphql
query {
  customer(id: 5) {
    name
    orders {
      id
      orderDate
      lineItems {
        product {
          name
        }
      }
    }
  }
}
```
- **Schema-driven traversal**: Client defines the depth and breadth.

## Enter GraphQL

- Developed by Facebook to solve REST limitations
- Single endpoint: `/graphql`
- Client defines exact data shape
- Strongly typed schema
- Supports queries, mutations, and subscriptions

## HotChocolate in .NET

- Open-source GraphQL server for .NET
- Easy integration with ASP.NET Core
- Schema-first or code-first
- Advanced features: filtering, sorting, projections, subscriptions

## Example GraphQL Query

```graphql
query {
  products {
    id
    name
    price
    category {
      name
    }
  }
}
```

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
    .AddDbContext<AppDbContext>()
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();
app.MapGraphQL();
app.Run();
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

type OrderLineItem {
  id: ID!
  quantity: Int!
  product: Product!
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

Register the mutation:

```csharp
builder.Services.AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();
```

## GraphQL Subscriptions

- Real-time updates over WebSocket
- Clients subscribe to events and receive data automatically
- Perfect for live dashboards or notifications

## Subscriptions Example

```graphql
subscription {
  onProductAdded {
    id
    name
    price
  }
}
```

## Subscriptions in HotChocolate

```csharp
public class Subscription
{
    [Subscribe]
    [Topic]
    public async Task<Product?> OnProductAdded([EventMessage] int id, AppDbContext db) => 
        await db.Products.FirstOrDefaultAsync(p => p.Id == id);
}

public class Mutation(AppDbContext db, [Service] ITopicEventSender sender)
{
    public async Task<Product> AddProduct(AddProductInput input)
    {
        var product = new Product { Name = input.Name, Price = input.Price };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        await sender.SendAsync(nameof(Subscription.OnProductAdded), product.Id);
        return product;
    }
}
```

## Adding Subscriptions Support

```csharp
builder.Services.AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddSubscriptionType<Subscription>()
    .AddInMemorySubscriptions(); // Simple in-memory provider

app.UseWebSockets();
app.MapGraphQL();
```

## Query Projection in GraphQL

- GraphQL lets the client specify which fields to return.
- HotChocolate supports automatic projection to EF Core.
- Prevents over-fetching and optimizes database queries.

## Example Query Projection

```graphql
query {
  customers {
    name
    orders {
      orderDate
    }
  }
}
```

HotChocolate translates this into a SQL query selecting only the necessary
columns and related data.

## Query Projection in Code

### Setup

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();
```
---

### Usage 

```csharp
public class Query(AppDbContext db)
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Product> GetProducts() => db.Products;

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Customer> GetCustomers() => db.Customers;
}
```

## Projections vs. DataLoaders

### When to use Projections (`[UseProjection]`)
- **Same Database:** EF Core can efficiently generate a `JOIN` or subquery.
- **Single Round-trip:** Fetching shallow 1:1 or 1:N relationships is usually faster in one query.
- **Simplicity:** Hot Chocolate handles the "stitching" automatically based on the query.

### When to use DataLoaders
- **Cross-Boundary:** Fetching from different sources (e.g., SQL + REST API).
- **Custom Logic:** When the resolver contains logic EF Core cannot translate to SQL.
- **Request Caching:** Prevents fetching the same entity multiple times in one request.
- **Scale:** Avoids "Cartesian Explosion" in extremely deep or complex nested queries.

## REST vs GraphQL Summary

| Feature    | REST                | GraphQL          |
| ---------- | ------------------- | ---------------- |
| Endpoint   | Multiple            | Single           |
| Data Shape | Fixed               | Client-defined   |
| Versioning | URL-based           | Schema evolution |
| Real-time  | Webhooks            | Subscriptions    |
| Efficiency | Over/Under fetching | Exact fetching   |

## When to Use Which?

**REST**

- Simpler public APIs
- Caching and CDN-friendly
- Easier for microservices

**GraphQL**

- Complex data relationships
- Multiple frontends (web, mobile)
- Real-time requirements
- Need for query flexibility

## Aggregating APIs: REST vs GraphQL

- Multi-service architectures often require clients to get data from multiple
  backends
- Two common approaches: API Gateway for REST, Schema Stitching for GraphQL

## Aggregating APIs - REST

<div class="mermaid">
  graph LR
   subgraph REST [REST API Aggregation]
        A[Client] -->|HTTP requests| B[API Gateway]
        B -->|Routes request| C[Service 1]
        B -->|Routes request| D[Service 2]
        B -->|Routes request| E[Service 3]
    end
</div>

## Aggregating APIs - GraphQL

<div class="mermaid">
    graph LR
    subgraph GraphQL [GraphQL Schema Stitching]
        F[Client] -->|GraphQL query| G[Unified GraphQL Schema]
        G -->|Delegates query| H[Service 1 GraphQL]
        G -->|Delegates query| I[Service 2 GraphQL]
        G -->|Delegates query| J[Service 3 GraphQL]
    end
</div>

## Aggregation Comparison

**REST → API Gateway**

- Single entry point routing requests to multiple REST services
- Handles cross-cutting concerns: auth, caching, rate-limiting
- Fixed data shapes per service; over-/under-fetching can occur
- Client still needs to make multiple requests to different services

## Aggregation Comparison

**GraphQL → Schema Stitching**

- Combines multiple GraphQL schemas into one unified schema
- Single strongly-typed GraphQL endpoint
- Clients can query multiple services in a single request
- Avoids multiple round-trips and over-/under-fetching
- Complexity increases with many services; requires careful dependency
  management

## Schema Stitching: Before — GraphQL Schemas

**Products Service**

```graphql
type Product {
  id: ID!
  name: String!
  price: Float!
}

type Query {
  products: [Product!]!
}
```

---

**Reviews Service**

```graphql
type Review {
  id: ID!
  productId: ID!
  rating: Int!
  comment: String
}

type Query {
  reviewsByProduct(productId: ID!): [Review!]!
}
```

- Services are independent
- Client must query products and reviews separately

## Schema Stitching: After — Unified GraphQL Schema

```graphql
type Product {
  id: ID!
  name: String!
  price: Float!
  reviews: [Review!]!   # delegated to Reviews Service
}

type Review {
  id: ID!
  rating: Int!
  comment: String
}

type Query {
  products: [Product!]!
}
```

---

**Client Query Example**

```graphql
query {
  products {
    id
    name
    price
    reviews {
      rating
      comment
    }
  }
}
```

- Single query hits multiple services
- `reviews` field added only at the gateway
- Services remain independent

## Conclusion: Key Takeaways

- **REST APIs**
  - Simple, mature, and widely supported
  - Works well with fixed data shapes and caching/CDNs
  - Easy to implement for microservices and public APIs
  - Limitations: over-/under-fetching, multiple round-trips for related data,
    versioning complexity

---

- **GraphQL APIs**
  - Flexible and client-driven: clients specify exact data requirements
  - Strongly-typed schema supports complex relationships
  - Reduces over-/under-fetching and multiple network calls
  - Supports real-time updates via subscriptions
  - Advanced features in HotChocolate: projections, filtering, sorting, schema
    stitching

---

- **Aggregating Multi-Service Architectures**
  - REST: API Gateway centralizes routing, auth, caching, and rate-limiting
  - GraphQL: Schema Stitching provides a unified, strongly-typed endpoint
  - GraphQL allows querying multiple services in a single request without
    breaking service boundaries

---

- **Choosing Between REST and GraphQL**
  - Use **REST** for simple, stable APIs with predictable data shapes
  - Use **GraphQL** for complex data relationships, multiple clients, or
    real-time requirements
  - HotChocolate in .NET makes implementing GraphQL practical and efficient

# Thank You

### Questions?
