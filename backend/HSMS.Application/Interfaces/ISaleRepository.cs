using HSMS.Application.DTOs;

namespace HSMS.Application.Interfaces;

public interface ISaleRepository
{
    Task<SaleResponseDTO> CreateSaleAsync(SaleCreateDTO sale, string soldBy);
}
