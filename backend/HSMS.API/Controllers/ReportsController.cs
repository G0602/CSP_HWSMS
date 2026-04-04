using HSMS.API.Auth;
using HSMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HSMS.API.Controllers;

[Route("api/reports")]
[ApiController]
public class ReportsController : ControllerBase
{
    private readonly ISaleRepository _saleRepository;

    public ReportsController(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    [Authorize(Policy = AuthPolicies.SalesRead)]
    [HttpGet("daily")]
    public async Task<IActionResult> GetDailySalesReport()
    {
        var report = await _saleRepository.GetDailySalesReportAsync();
        return Ok(report);
    }

    [Authorize(Policy = AuthPolicies.SalesRead)]
    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthlySalesReport()
    {
        var report = await _saleRepository.GetMonthlySalesReportAsync();
        return Ok(report);
    }
}
