using HSMS.Application.DTOs;

namespace HSMS.Application.Interfaces;

public interface ISupplierRepository
{
    Task<int> AddSupplierAsync(SupplierCreateDTO dto);
}
