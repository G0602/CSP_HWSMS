namespace HSMS.Application.DTOs;

public class ReportsSummaryResponseDTO
{
    public List<DailySalesReportItemDTO> Daily { get; set; } = [];
    public List<MonthlySalesReportItemDTO> Monthly { get; set; } = [];
    public List<InventoryProductResponseDTO> LowStock { get; set; } = [];
}
