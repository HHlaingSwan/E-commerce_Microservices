using Microsoft.EntityFrameworkCore;
using InventoryService.Context;
using InventoryService.Repositories;
using InventoryService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddDbContext<InventoryDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    db.Database.EnsureCreated();
}

app.MapGrpcService<InventoryServiceImpl>();
app.MapGet("/", () => "InventoryService gRPC");

app.Run();
