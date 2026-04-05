namespace HSMS.Application.DTOs;

/// <summary>
/// DTO for manual stock updates of a single product.
/// </summary>
public class ProductStockUpdateDTO
{
    /// <summary>
    /// The new absolute quantity to store for the product.
    /// Must be zero or greater.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Optional operator note for audit logs.
    /// </summary>
    public string? Reason { get; set; }
}
