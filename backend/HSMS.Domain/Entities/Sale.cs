namespace HSMS.Domain.Entities;

public class Sale
{
    public int Id { get; set; }

    public DateTime SoldAt { get; set; }

    public decimal TotalAmount { get; set; }

    public string SoldBy { get; set; } = string.Empty;

    public List<SaleItem> Items { get; set; } = new();
}
