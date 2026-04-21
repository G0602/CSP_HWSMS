using HSMS.Application.DTOs;
using HSMS.Domain.Entities;

namespace HSMS.Application.Interfaces;

public interface ISupplierRepository
{
    Task<List<Supplier>> GetSuppliersAsync();
    Task<bool> SupplierExistsAsync(int id);
    Task<int> AddSupplierAsync(SupplierCreateDTO dto);
    Task<bool> UpdateSupplierAsync(int id, SupplierUpdateDTO dto);
    Task<SupplierDeleteStatus> DeleteSupplierAsync(int id);
}

public enum SupplierDeleteStatus
{
    Deleted,
    NotFound,
    LinkedRecordsExist
}
