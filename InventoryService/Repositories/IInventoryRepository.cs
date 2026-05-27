using InventoryService.Entities;

namespace InventoryService.Repositories;

public interface IInventoryRepository
{
    Task<InventoryItem?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
