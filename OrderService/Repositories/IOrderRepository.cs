using OrderService.Entities;

namespace OrderService.Repositories;

public interface IOrderRepository
{
    Task AddAsync(Entities.Order order, CancellationToken cancellationToken = default);
    Task<Entities.Order?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<List<Entities.Order>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
