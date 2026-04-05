using HSMS.API.Auth;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HSMS.API.Controllers;

[Route("api/suppliers")]
[ApiController]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierRepository _supplierRepository;

    public SuppliersController(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    [Authorize(Policy = AuthPolicies.InventoryRead)]
    [HttpGet]
    public async Task<IActionResult> GetSuppliers()
    {
        var suppliers = await _supplierRepository.GetSuppliersAsync();
        return Ok(suppliers);
    }

    [Authorize(Policy = AuthPolicies.InventoryWrite)]
    [HttpPost]
    public async Task<IActionResult> AddSupplier(SupplierCreateDTO dto)
    {
        string name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Name is required.");
        }

        try
        {
            int id = await _supplierRepository.AddSupplierAsync(new SupplierCreateDTO 
            { 
                Name = name,
                ContactInfo = dto.ContactInfo?.Trim()
            });
            return Created($"/api/suppliers/{id}", new { id, name });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [Authorize(Policy = AuthPolicies.InventoryWrite)]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSupplier(int id, SupplierUpdateDTO dto)
    {
        string name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Name is required.");
        }

        try
        {
            bool updated = await _supplierRepository.UpdateSupplierAsync(id, new SupplierUpdateDTO 
            { 
                Name = name,
                ContactInfo = dto.ContactInfo?.Trim()
            });
            if (!updated)
            {
                return NotFound("Supplier not found.");
            }

            return Ok("Supplier updated successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [Authorize(Policy = AuthPolicies.InventoryWrite)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSupplier(int id)
    {
        var status = await _supplierRepository.DeleteSupplierAsync(id);

        return status switch
        {
            SupplierDeleteStatus.Deleted => Ok("Supplier deleted successfully."),
            SupplierDeleteStatus.LinkedRecordsExist => Conflict("Cannot delete supplier because it is linked to existing records."),
            _ => NotFound("Supplier not found.")
        };
    }
}
