using ECommerce.Contracts.Interceptors;
using Inventory;
using Microsoft.EntityFrameworkCore;
using OrderService.Context;
using OrderService.Repositories;
using OrderService.Services;
using Product;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddDbContext<OrderDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services
    .AddGrpcClient<global::Product.ProductService.ProductServiceClient>(o =>
        o.Address = new Uri(builder.Configuration["Services:ProductService"]!))
    .AddInterceptor<DeadlinePropagationInterceptor>();

builder.Services
    .AddGrpcClient<global::Inventory.InventoryService.InventoryServiceClient>(o =>
        o.Address = new Uri(builder.Configuration["Services:InventoryService"]!))
    .AddInterceptor<DeadlinePropagationInterceptor>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.EnsureCreated();
}

app.MapGrpcService<OrderServiceImpl>();
app.MapGet("/", () => "OrderService gRPC");

app.Run();
