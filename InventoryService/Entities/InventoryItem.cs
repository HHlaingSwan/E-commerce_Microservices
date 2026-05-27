namespace InventoryService.Entities;

public class InventoryItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int AvailableQuantity { get; set; }
    public int ReservedQuantity { get; set; }
}
