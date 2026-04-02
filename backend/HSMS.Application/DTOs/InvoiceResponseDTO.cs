namespace HSMS.Application.DTOs;

public class InvoiceResponseDTO
{
    public int TransactionId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime SoldAt { get; set; }

    public string SoldBy { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }

    public decimal TaxRate { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal GrandTotal { get; set; }

    public List<InvoiceItemDTO> Items { get; set; } = new();
}
