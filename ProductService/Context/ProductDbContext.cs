using Microsoft.EntityFrameworkCore;
using ProductService.Entities;

namespace ProductService.Context;

public class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<Entities.Product> Products => Set<Entities.Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entities.Product>(e =>
        {
            e.HasIndex(p => p.Name);
            e.HasIndex(p => p.Category);
        });
    }
}
