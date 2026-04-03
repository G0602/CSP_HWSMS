using HSMS.Application.DTOs;

namespace HSMS.Application.Interfaces;

public interface ISupplierRepository
{
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
