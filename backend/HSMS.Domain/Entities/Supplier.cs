namespace HSMS.Domain.Entities;

/// <summary>
/// Represents a product supplier.
/// </summary>
public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional contact information (phone, email, or other).
    /// </summary>
    public string? ContactInfo { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
