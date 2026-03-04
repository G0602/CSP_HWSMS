namespace HSMS.Domain.Entities;

/// <summary>
/// Represents a product in the hardware store inventory.
/// This is the core domain entity used throughout the application.
/// </summary>
public class Product
{
    /// <summary>
    /// The unique auto-incremented identifier for the product.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The display name of the product (e.g., "Claw Hammer").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The Stock Keeping Unit — a unique alphanumeric code used to
    /// track and identify each product in the inventory.
    /// </summary>
    public string SKU { get; set; } = string.Empty;

    /// <summary>
    /// The selling price of the product in LKR (Sri Lankan Rupees).
    /// Must be greater than zero.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// The number of units currently in stock.
    /// Cannot be negative.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// The product category (e.g., "Hand Tools", "Power Tools", "Fasteners").
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// The UTC timestamp of when the product record was created in the database.
    /// Set automatically by MySQL's DEFAULT CURRENT_TIMESTAMP.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}