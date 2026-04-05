namespace HSMS.Application.DTOs;

/// <summary>
/// DTO used to create a supplier.
/// </summary>
public class SupplierCreateDTO
{
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional contact information (phone, email, or other).
    /// </summary>
    public string? ContactInfo { get; set; }
}
