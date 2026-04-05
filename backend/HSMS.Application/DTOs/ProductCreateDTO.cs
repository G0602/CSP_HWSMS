namespace HSMS.Application.DTOs;

/// <summary>
/// Data Transfer Object used when creating a new product.
/// Contains all fields required to persist a new product record.
/// </summary>
public class ProductCreateDTO
{
    /// <summary>The display name of the new product.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The unique SKU (Stock Keeping Unit) code for the product.</summary>
    public string SKU { get; set; } = string.Empty;

    /// <summary>The selling price. Must be greater than zero.</summary>
    public decimal Price { get; set; }

    /// <summary>The initial stock quantity. Cannot be negative.</summary>
    public int Quantity { get; set; }

    /// <summary>The product category (e.g., "Hand Tools", "Fasteners").</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>The optional supplier Id linked to this product.</summary>
    public int? SupplierId { get; set; }
}
