namespace HSMS.Application.DTOs;

public class SaleResponseDTO
{
    public int SaleId { get; set; }

    public DateTime SoldAt { get; set; }

    public decimal TotalAmount { get; set; }

    public string SoldBy { get; set; } = string.Empty;

    public List<SaleItemResponseDTO> Items { get; set; } = new();
}
