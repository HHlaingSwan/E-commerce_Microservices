using Grpc.Core;
using Inventory;
using InventoryService.Repositories;
using static Inventory.InventoryService;

namespace InventoryService.Services;

public class InventoryServiceImpl(
    IInventoryRepository inventoryRepo,
    ILogger<InventoryServiceImpl> logger) : InventoryServiceBase
{
    public override async Task<StockCheckResponse> CheckStock(StockCheckRequest request, ServerCallContext context)
    {
        var item = await inventoryRepo.GetByProductIdAsync(request.ProductId, context.CancellationToken);

        return new StockCheckResponse
        {
            ProductId = request.ProductId,
            AvailableQuantity = item?.AvailableQuantity ?? 0
        };
    }

    public override async Task<ReserveStockResponse> ReserveStock(ReserveStockRequest request, ServerCallContext context)
    {
        var item = await inventoryRepo.GetByProductIdAsync(request.ProductId, context.CancellationToken);

        if (item is null)
            return Fail($"Product {request.ProductId} not found in inventory");

        if (item.AvailableQuantity < request.Quantity)
            return Fail($"Insufficient stock for product {request.ProductId}. Available: {item.AvailableQuantity}, requested: {request.Quantity}");

        item.AvailableQuantity -= request.Quantity;
        item.ReservedQuantity += request.Quantity;
        await inventoryRepo.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Reserved {Qty} of product {ProductId} for order {OrderId}",
            request.Quantity, request.ProductId, request.OrderId);

        return new ReserveStockResponse { Success = true };
    }

    public override async Task<Empty> ReleaseStock(ReleaseStockRequest request, ServerCallContext context)
    {
        var item = await inventoryRepo.GetByProductIdAsync(request.ProductId, context.CancellationToken);

        if (item is not null)
        {
            item.ReservedQuantity -= request.Quantity;
            item.AvailableQuantity += request.Quantity;
            await inventoryRepo.SaveChangesAsync(context.CancellationToken);

            logger.LogInformation("Released {Qty} of product {ProductId} for order {OrderId}",
                request.Quantity, request.ProductId, request.OrderId);
        }

        return new Empty();
    }

    private static ReserveStockResponse Fail(string reason) =>
        new() { Success = false, FailureReason = reason };
}
