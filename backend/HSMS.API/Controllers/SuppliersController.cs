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

    [Authorize(Policy = AuthPolicies.InventoryWrite)]
    [HttpPost]
    public async Task<IActionResult> AddSupplier(SupplierCreateDTO dto)
    {
        string name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Name is required.");
        }

        int id = await _supplierRepository.AddSupplierAsync(new SupplierCreateDTO { Name = name });
        return Created($"/api/suppliers/{id}", new { id, name });
    }
}
