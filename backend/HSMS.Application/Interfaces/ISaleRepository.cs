using HSMS.Application.DTOs;

namespace HSMS.Application.Interfaces;

public interface ISaleRepository
{
    Task<SaleResponseDTO> CreateSaleAsync(SaleCreateDTO sale, string soldBy);

    Task<List<SaleHistoryItemDTO>> GetSalesHistoryAsync(int? saleId, DateTime? fromDate, DateTime? toDate, int limit = 100);

    Task<SaleResponseDTO?> GetSaleDetailsAsync(int saleId);
}
