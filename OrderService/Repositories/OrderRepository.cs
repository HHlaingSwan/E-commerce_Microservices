using Microsoft.EntityFrameworkCore;
using OrderService.Context;
using OrderService.Entities;

namespace OrderService.Repositories;

public class OrderRepository(OrderDbContext db) : IOrderRepository
{
    public async Task AddAsync(Entities.Order order, CancellationToken cancellationToken = default)
    {
        await db.Orders.AddAsync(order, cancellationToken);
    }

    public async Task<Entities.Order?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<List<Entities.Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await db.Orders
            .Include(o => o.Items)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await db.SaveChangesAsync(cancellationToken);
    }
}
