namespace HSMS.Application.DTOs;

/// <summary>
/// Inventory-focused product payload with stock status metadata.
/// </summary>
public class InventoryProductResponseDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? SupplierId { get; set; }
    public bool IsLowStock { get; set; }
}
