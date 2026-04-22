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
    private readonly decimal _reportCostRatio;

    public ReportsController(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        IConfiguration? configuration = null)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _lowStockThreshold = Math.Max(1, configuration?.GetValue<int?>("LOW_STOCK_THRESHOLD") ?? 10);
        _reportCostRatio = decimal.Clamp(configuration?.GetValue<decimal?>("REPORT_COST_RATIO") ?? 0.70m, 0m, 1m);
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

    [Authorize(Policy = AuthPolicies.SalesRead)]
    [HttpGet("analytics")]
    public async Task<IActionResult> GetSalesAnalytics(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int? productId,
        [FromQuery] string? category)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
        {
            return BadRequest("From date cannot be after to date.");
        }

        var analytics = await _saleRepository.GetSalesAnalyticsAsync(
            fromDate,
            toDate,
            productId,
            category,
            _reportCostRatio);

        return Ok(analytics);
    }

    [Authorize(Policy = AuthPolicies.InventoryManagerRead)]
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockReport()
    {
        var products = await _productRepository.GetLowStockProducts(_lowStockThreshold);
        var lowStockProducts = products
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
    [HttpGet("summary")]
    public async Task<IActionResult> GetReportsSummary()
    {
        var dailyTask = _saleRepository.GetDailySalesReportAsync();
        var monthlyTask = _saleRepository.GetMonthlySalesReportAsync();
        var lowStockTask = _productRepository.GetLowStockProducts(_lowStockThreshold);

        await Task.WhenAll(dailyTask, monthlyTask, lowStockTask);

        var response = new ReportsSummaryResponseDTO
        {
            Daily = dailyTask.Result,
            Monthly = monthlyTask.Result,
            LowStock = lowStockTask.Result
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
                })
                .ToList()
        };

        return Ok(response);
    }

    [Authorize(Policy = AuthPolicies.SalesRead)]
    [HttpGet("export")]
    public async Task<IActionResult> ExportReport([FromQuery] string type = "daily")
    {
        string reportType = type.Trim().ToLowerInvariant();
        string csvContent;
        string fileNamePrefix;

        switch (reportType)
        {
            case "daily":
            {
                var dailyReport = await _saleRepository.GetDailySalesReportAsync();
                var csv = new StringBuilder();
                csv.AppendLine("Date,TotalSales");

                foreach (var item in dailyReport)
                {
                    csv.AppendLine($"{item.Date:yyyy-MM-dd},{item.TotalAmount:0.00}");
                }

                csvContent = csv.ToString();
                fileNamePrefix = "daily-sales-report";
                break;
            }
            case "monthly":
            {
                var monthlyReport = await _saleRepository.GetMonthlySalesReportAsync();
                var csv = new StringBuilder();
                csv.AppendLine("Month,TotalSales");

                foreach (var item in monthlyReport)
                {
                    csv.AppendLine($"{item.Month:yyyy-MM},{item.TotalAmount:0.00}");
                }

                csvContent = csv.ToString();
                fileNamePrefix = "monthly-sales-report";
                break;
            }
            case "low-stock":
            {
                var lowStockProducts = (await _productRepository.GetLowStockProducts(_lowStockThreshold))
                    .OrderBy(product => product.Name)
                    .ToList();

                var csv = new StringBuilder();
                csv.AppendLine("Product,SKU,Category,Quantity,Price");

                foreach (var product in lowStockProducts)
                {
                    csv.AppendLine($"{EscapeCsv(product.Name)},{EscapeCsv(product.SKU)},{EscapeCsv(product.Category)},{product.Quantity},{product.Price:0.00}");
                }

                csvContent = csv.ToString();
                fileNamePrefix = "low-stock-report";
                break;
            }
            default:
                return BadRequest("Unsupported export type. Use type=daily, type=monthly, or type=low-stock.");
        }

        var bytes = Encoding.UTF8.GetBytes(csvContent);
        var fileName = $"{fileNamePrefix}-{DateTime.UtcNow:yyyyMMdd}.csv";

        return File(bytes, "text/csv", fileName);
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        bool requiresQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        if (!requiresQuotes)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
