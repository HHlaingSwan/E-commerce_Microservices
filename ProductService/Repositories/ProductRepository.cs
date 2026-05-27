using Microsoft.EntityFrameworkCore;
using ProductService.Context;
using ProductService.Entities;

namespace ProductService.Repositories;

public class ProductRepository(ProductDbContext db) : IProductRepository
{
    public async Task<Entities.Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await db.Products.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<List<Entities.Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await db.Products.ToListAsync(cancellationToken);
    }
}
