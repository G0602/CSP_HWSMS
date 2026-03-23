namespace HSMS.Application.DTOs;

/// <summary>
/// Data Transfer Object used when updating an existing product.
/// All fields are replaceable — only non-empty / non-zero values are meaningful.
/// The product Id is supplied via the route parameter, not in this DTO.
/// </summary>
public class ProductUpdateDTO
{
    /// <summary>The new display name for the product.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The new SKU code. Must remain unique across the inventory.</summary>
    public string SKU { get; set; } = string.Empty;

    /// <summary>The new selling price. Must be greater than zero.</summary>
    public decimal Price { get; set; }

    /// <summary>The updated stock quantity. Cannot be negative.</summary>
    public int Quantity { get; set; }

    /// <summary>The updated product category.</summary>
    public string Category { get; set; } = string.Empty;
}
