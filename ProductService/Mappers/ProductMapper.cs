using Product;
using ProductService.Entities;

namespace ProductService.Mappers;

public static class ProductMapper
{
    public static ProductResponse ToResponse(this Entities.Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Price = (double)p.Price,
        Category = p.Category
    };
}
