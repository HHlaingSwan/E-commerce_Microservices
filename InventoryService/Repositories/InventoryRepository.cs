using Microsoft.EntityFrameworkCore;
using InventoryService.Context;
using InventoryService.Entities;

namespace InventoryService.Repositories;

public class InventoryRepository(InventoryDbContext db) : IInventoryRepository
{
    public async Task<InventoryItem?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await db.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await db.SaveChangesAsync(cancellationToken);
    }
}
