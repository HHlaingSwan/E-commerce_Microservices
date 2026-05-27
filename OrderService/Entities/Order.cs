using System.ComponentModel.DataAnnotations;

namespace OrderService.Entities;

public class Order
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public decimal Total { get; set; }

    public List<OrderItem> Items { get; set; } = [];
}
