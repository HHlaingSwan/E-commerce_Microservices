using Grpc.Core;
using Product;
using ProductService.Mappers;
using ProductService.Repositories;
using static Product.ProductService;

namespace ProductService.Services;

public class ProductServiceImpl(
    IProductRepository productRepo,
    ILogger<ProductServiceImpl> logger) : ProductServiceBase
{
    public override async Task<ProductResponse> GetProduct(ProductRequest request, ServerCallContext context)
    {
        var product = await productRepo.GetByIdAsync(request.Id, context.CancellationToken);

        if (product is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Product {request.Id} not found"));

        logger.LogInformation("Product {Id} retrieved: {Name}", product.Id, product.Name);

        return product.ToResponse();
    }

    public override async Task<ProductListResponse> ListProducts(Empty request, ServerCallContext context)
    {
        var products = await productRepo.GetAllAsync(context.CancellationToken);

        var response = new ProductListResponse();
        response.Products.AddRange(products.Select(p => p.ToResponse()));
        return response;
    }
}
