using ProductService.Entities;

namespace ProductService.Repositories;

public interface IProductRepository
{
    Task<Entities.Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Entities.Product>> GetAllAsync(CancellationToken cancellationToken = default);
}
