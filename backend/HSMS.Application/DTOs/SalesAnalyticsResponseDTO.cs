namespace HSMS.Application.DTOs;

public class SalesAnalyticsResponseDTO
{
    public decimal TotalSales { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalProfit { get; set; }
    public List<DailySalesAnalyticsItemDTO> DailyTrends { get; set; } = [];
    public List<MonthlySalesAnalyticsItemDTO> MonthlyTrends { get; set; } = [];
}

public class DailySalesAnalyticsItemDTO
{
    public DateTime Date { get; set; }
    public decimal Sales { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
}

public class MonthlySalesAnalyticsItemDTO
{
    public DateTime Month { get; set; }
    public decimal Sales { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
}