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

#### With examples in ASP.NET Core and HotChocolate

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
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    [HttpGet]
    public IEnumerable<Product> Get() => _service.GetAll();

    [HttpGet("{id}")]
    public Product Get(int id) => _service.GetById(id);

    [HttpPost]
    public async Task<IActionResult> Create(Product product)
    {
        await _service.Create(product);
        return Created(new { id = product.Id }, product);
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
public class Query
{
    public IQueryable<Product> GetProducts([Service] AppDbContext db) => db.Products;
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
  category: Category
}

type Category {
  id: ID!
  name: String!
}

type Query {
  products: [Product!]!
}
```

## GraphQL Mutations

Mutations allow data modification.

```graphql
mutation {
  addProduct(input: { name: "Keyboard", price: 49.99, categoryId: 2 }) {
    id
    name
  }
}
```

## Mutations in HotChocolate

```csharp
public record AddProductInput(string Name, double Price, int CategoryId);

public class Mutation
{
    public async Task<Product> AddProduct(AddProductInput input, [Service] AppDbContext db)
    {
        var product = new Product { Name = input.Name, Price = input.Price, CategoryId = input.CategoryId };
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
    public Product OnProductAdded([EventMessage] Product product) => product;
}

public class Mutation
{
    public async Task<Product> AddProduct(
        AddProductInput input,
        [Service] AppDbContext db,
        [Service] ITopicEventSender sender)
    {
        var product = new Product { Name = input.Name, Price = input.Price, CategoryId = input.CategoryId };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        await sender.SendAsync(nameof(Subscription.OnProductAdded), product);
        return product;
    }
}
```

## Adding Subscriptions Support

```csharp
builder.Services.AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddSubscriptionType<Subscription>();

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
  products {
    name
    category {
      name
    }
  }
}
```

HotChocolate translates this into a SQL query selecting only the necessary
columns.

## Query Projection in Code

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();
```

```csharp
public class Query
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Product> GetProducts([Service] AppDbContext db) => db.Products;
}
```

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
