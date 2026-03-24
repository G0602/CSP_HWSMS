namespace HSMS.Application.DTOs;

public class SaleHistoryItemDTO
{
    public int SaleId { get; set; }

    public DateTime SoldAt { get; set; }

    public decimal TotalAmount { get; set; }

    public string SoldBy { get; set; } = string.Empty;

    public int ItemCount { get; set; }
}
