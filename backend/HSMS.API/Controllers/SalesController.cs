using System.Security.Claims;
using HSMS.API.Auth;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HSMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SalesController : ControllerBase
{
    private readonly ISaleRepository _saleRepository;

    public SalesController(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    [Authorize(Policy = AuthPolicies.SalesCreate)]
    [HttpPost]
    public async Task<IActionResult> CreateSale(SaleCreateDTO dto)
    {
        if (dto.Items.Count == 0)
        {
            return BadRequest("Please add at least one item to the sale.");
        }

        string soldBy = User.FindFirstValue(ClaimTypes.Name) ?? "unknown-user";

        try
        {
            SaleResponseDTO sale = await _saleRepository.CreateSaleAsync(dto, soldBy);
            return Ok(sale);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch
        {
            return StatusCode(500, "Failed to save sale transaction.");
        }
    }

    [Authorize(Policy = AuthPolicies.SalesRead)]
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int? transactionId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int limit = 100)
    {
        if (limit <= 0)
        {
            return BadRequest("Limit must be greater than zero.");
        }

        var history = await _saleRepository.GetSalesHistoryAsync(transactionId, fromDate, toDate, limit);
        return Ok(history);
    }

    [Authorize(Policy = AuthPolicies.SalesRead)]
    [HttpGet("{saleId:int}")]
    public async Task<IActionResult> GetDetails(int saleId)
    {
        var sale = await _saleRepository.GetSaleDetailsAsync(saleId);
        if (sale is null)
        {
            return NotFound("Transaction not found.");
        }

        return Ok(sale);
    }

    [Authorize(Policy = AuthPolicies.SalesRead)]
    [HttpGet("{saleId:int}/invoice")]
    public async Task<IActionResult> GetInvoice(int saleId)
    {
        var invoice = await _saleRepository.GetInvoiceAsync(saleId);
        if (invoice is null)
        {
            return NotFound("Transaction not found.");
        }

        return Ok(invoice);
    }
}
