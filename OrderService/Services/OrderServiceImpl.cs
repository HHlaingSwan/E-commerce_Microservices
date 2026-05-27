using Grpc.Core;
using Inventory;
using OrderService.Repositories;
using Product;

namespace OrderService.Services;

public class OrderServiceImpl(
    IOrderRepository orderRepo,
    Product.ProductService.ProductServiceClient productClient,
    Inventory.InventoryService.InventoryServiceClient inventoryClient,
    ILogger<OrderServiceImpl> logger) : Order.OrderService.OrderServiceBase
{
    public override async Task<Order.OrderResponse> CreateOrder(Order.CreateOrderRequest request, ServerCallContext context)
    {
        var order = new Entities.Order { Id = Guid.NewGuid().ToString() };
        var reserved = new List<(int productId, int quantity)>();

        try
        {
            foreach (var item in request.Items)
            {
                var product = await productClient.GetProductAsync(
                    new ProductRequest { Id = item.ProductId },
                    deadline: context.Deadline);

                order.Items.Add(new Entities.OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = (decimal)product.Price
                });

                var reserve = await inventoryClient.ReserveStockAsync(
                    new ReserveStockRequest
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        OrderId = order.Id
                    },
                    deadline: context.Deadline);

                if (!reserve.Success)
                {
                    await ReleaseReserved(inventoryClient, reserved);
                    throw new RpcException(new Status(
                        StatusCode.FailedPrecondition,
                        reserve.FailureReason));
                }

                reserved.Add((item.ProductId, item.Quantity));
            }

            order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);
            order.Status = "Confirmed";

            await orderRepo.AddAsync(order, context.CancellationToken);
            await orderRepo.SaveChangesAsync(context.CancellationToken);

            logger.LogInformation("Order {OrderId} created with {ItemCount} items, total {Total}",
                order.Id, order.Items.Count, order.Total);

            return MapOrder(order);
        }
        catch
        {
            await ReleaseReserved(inventoryClient, reserved);
            throw;
        }
    }

    public override async Task<Order.OrderResponse> GetOrder(Order.GetOrderRequest request, ServerCallContext context)
    {
        var order = await orderRepo.GetByIdAsync(request.OrderId, context.CancellationToken);

        if (order is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Order {request.OrderId} not found"));

        return MapOrder(order);
    }

    public override async Task<Order.OrderListResponse> ListOrders(Order.ListOrdersRequest request, ServerCallContext context)
    {
        var orders = await orderRepo.GetAllAsync(context.CancellationToken);

        var response = new Order.OrderListResponse();
        response.Orders.AddRange(orders.Select(MapOrder));
        return response;
    }

    private static async Task ReleaseReserved(Inventory.InventoryService.InventoryServiceClient client, List<(int productId, int quantity)> items)
    {
        foreach (var (productId, quantity) in items)
        {
            await client.ReleaseStockAsync(new ReleaseStockRequest
            {
                ProductId = productId,
                Quantity = quantity
            });
        }
    }

    private static Order.OrderResponse MapOrder(Entities.Order o)
    {
        var response = new Order.OrderResponse
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
