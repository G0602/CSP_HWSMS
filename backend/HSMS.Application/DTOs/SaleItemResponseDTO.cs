namespace HSMS.Application.DTOs;

public class SaleItemResponseDTO
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineSubtotal { get; set; }
}
