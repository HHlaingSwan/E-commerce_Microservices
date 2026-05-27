using System.ComponentModel.DataAnnotations;

namespace ProductService.Entities;

public class Product
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;
}
