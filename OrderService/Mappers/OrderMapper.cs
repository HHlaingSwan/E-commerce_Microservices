using Order;
using OrderService.Entities;

namespace OrderService.Mappers;

public static class OrderMapper
{
    public static OrderResponse ToResponse(this Entities.Order o)
    {
        var response = new OrderResponse
        {
            OrderId = o.Id,
            Status = o.Status,
            Total = (double)o.Total,
            CreatedAt = o.CreatedAt.ToString("O")
        };
        response.Items.AddRange(o.Items.Select(i => new Order.OrderItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity
        }));
        return response;
    }
}
