#region

using System.Text.Json.Serialization;
using demo.API.GraphQL;
using demo.API.GraphQL.DataLoaders;
using demo.API.GraphQL.Resolvers;
using demo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

#endregion

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddPooledDbContextFactory<AppDbContext>(options =>
    options.UseSqlite("Data Source=demo.db"));

// For simplicity in non-GQL parts (like initialization), register the DbContext too.
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

// REST API Controller configuration
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// Swagger configuration for REST API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- GraphQL Configuration ---
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddSubscriptionType<Subscription>()

    // Register Resolvers
    .AddTypeExtension<CustomerResolvers>()
    .AddTypeExtension<OrderResolvers>()
    .AddTypeExtension<OrderLineItemResolvers>()

    // Register DataLoaders
    .AddDataLoader<CustomerByIdDataLoader>()
    .AddDataLoader<OrdersByCustomerIdDataLoader>()
    .AddDataLoader<OrderLineItemsByOrderIdDataLoader>()
    .AddDataLoader<ProductByIdDataLoader>()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .AddInMemorySubscriptions();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseWebSockets();
app.UseHttpsRedirection();

// Initialize and Seed Database on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbInitializer.Initialize(db);
}

app.MapControllers();
app.MapGraphQL();

app.Run();