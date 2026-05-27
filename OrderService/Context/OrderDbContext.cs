using Microsoft.EntityFrameworkCore;
using OrderService.Entities;

namespace OrderService.Context;

public class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<Entities.Order> Orders => Set<Entities.Order>();
    public DbSet<Entities.OrderItem> OrderItems => Set<Entities.OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entities.Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.Status);
            e.HasIndex(o => o.CreatedAt);
            e.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId);
        });

        modelBuilder.Entity<Entities.OrderItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.HasIndex(i => i.OrderId);
            e.HasIndex(i => i.ProductId);
        });
    }
}
