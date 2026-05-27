using Microsoft.EntityFrameworkCore;
using InventoryService.Entities;

namespace InventoryService.Context;

public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>(e =>
        {
            e.HasIndex(i => i.ProductId).IsUnique();
        });
    }
}
