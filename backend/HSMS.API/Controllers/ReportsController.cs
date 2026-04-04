using HSMS.API.Auth;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace HSMS.API.Controllers;

[Route("api/reports")]
[ApiController]
public class ReportsController : ControllerBase
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly int _lowStockThreshold;

    public ReportsController(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        IConfiguration? configuration = null)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _lowStockThreshold = Math.Max(1, configuration?.GetValue<int?>("LOW_STOCK_THRESHOLD") ?? 10);
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

    [Authorize(Policy = AuthPolicies.InventoryManagerRead)]
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockReport()
    {
        var products = await _productRepository.GetAllProducts();
        var lowStockProducts = products
            .Where(product => product.Quantity < _lowStockThreshold)
            .Select(product => new InventoryProductResponseDTO
            {
                Id = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                Quantity = product.Quantity,
                Category = product.Category,
                Price = product.Price,
                SupplierId = product.SupplierId,
                IsLowStock = true
            });

        return Ok(lowStockProducts);
    }

    [Authorize(Policy = AuthPolicies.SalesRead)]
    [HttpGet("export")]
    public async Task<IActionResult> ExportReport([FromQuery] string type = "daily")
    {
        string reportType = type.Trim().ToLowerInvariant();
        if (reportType != "daily")
        {
            return BadRequest("Unsupported export type. Use type=daily.");
        }

        var dailyReport = await _saleRepository.GetDailySalesReportAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Date,TotalSales");

        foreach (var item in dailyReport)
        {
            csv.AppendLine($"{item.Date:yyyy-MM-dd},{item.TotalAmount:0.00}");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"daily-sales-report-{DateTime.UtcNow:yyyyMMdd}.csv";

        return File(bytes, "text/csv", fileName);
    }
}
