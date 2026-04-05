using Xunit;
using Moq;
using HSMS.API.Controllers;
using HSMS.Application.Interfaces;
using HSMS.Application.DTOs;
using HSMS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSMS.Tests;

/// <summary>
/// Sprint 3 - EPIC 3.4: Reporting Module Unit Tests
/// Tests for stories: S3-US-10, S3-US-11, S3-US-12, S3-US-13
/// </summary>
public class ReportingModuleTests
{
    private static IConfiguration CreateConfiguration(int? lowStockThreshold = 10)
    {
        var config = new Dictionary<string, string?>
        {
            { "LOW_STOCK_THRESHOLD", lowStockThreshold?.ToString() ?? "10" }
        };
        return new ConfigurationBuilder().AddInMemoryCollection(config).Build();
    }

    #region Story S3-US-10: Daily Sales Report

    [Fact]
    public async Task GetDailySalesReport_Should_Return_Daily_Totals()
    {
        // Arrange
        var mockSaleRepo = new Mock<ISaleRepository>();
        var mockProductRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration();

        var dailyReport = new List<DailySalesReportItemDTO>
        {
            new() { Date = System.DateTime.Parse("2026-04-01"), TotalAmount = 5000 },
            new() { Date = System.DateTime.Parse("2026-04-02"), TotalAmount = 3500 }
        };

        mockSaleRepo.Setup(r => r.GetDailySalesReportAsync()).ReturnsAsync(dailyReport);

        var controller = new ReportsController(mockSaleRepo.Object, mockProductRepo.Object, config);

        // Act
        var result = await controller.GetDailySalesReport();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var report = ((IEnumerable<DailySalesReportItemDTO>)okResult.Value!).ToList();
        Assert.Equal(2, report.Count);
        Assert.Equal(5000, report[0].TotalAmount);
    }

    [Fact]
    public async Task GetDailySalesReport_Should_Aggregate_By_Date()
    {
        // Arrange
        var mockSaleRepo = new Mock<ISaleRepository>();
        var mockProductRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration();

        var dailyReport = new List<DailySalesReportItemDTO>
        {
            new() { Date = System.DateTime.Parse("2026-04-01"), TotalAmount = 10000 }
        };

        mockSaleRepo.Setup(r => r.GetDailySalesReportAsync()).ReturnsAsync(dailyReport);

        var controller = new ReportsController(mockSaleRepo.Object, mockProductRepo.Object, config);

        // Act
        var result = await controller.GetDailySalesReport();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var report = ((IEnumerable<DailySalesReportItemDTO>)okResult.Value!).ToList();
        Assert.Single(report);
    }

    [Fact]
    public async Task GetDailySalesReport_Should_Return_Empty_When_No_Sales()
    {
        // Arrange
        var mockSaleRepo = new Mock<ISaleRepository>();
        var mockProductRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration();

        mockSaleRepo.Setup(r => r.GetDailySalesReportAsync()).ReturnsAsync(new List<DailySalesReportItemDTO>());

        var controller = new ReportsController(mockSaleRepo.Object, mockProductRepo.Object, config);

        // Act
        var result = await controller.GetDailySalesReport();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var report = ((IEnumerable<DailySalesReportItemDTO>)okResult.Value!).ToList();
        Assert.Empty(report);
    }

    #endregion

    #region Story S3-US-11: Monthly Sales Report

    [Fact]
    public async Task GetMonthlySalesReport_Should_Return_Monthly_Totals()
    {
        // Arrange
        var mockSaleRepo = new Mock<ISaleRepository>();
        var mockProductRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration();

        var monthlyReport = new List<MonthlySalesReportItemDTO>
        {
            new() { Month = System.DateTime.Parse("2026-04-01"), TotalAmount = 50000 },
            new() { Month = System.DateTime.Parse("2026-05-01"), TotalAmount = 45000 }
        };

        mockSaleRepo.Setup(r => r.GetMonthlySalesReportAsync()).ReturnsAsync(monthlyReport);

        var controller = new ReportsController(mockSaleRepo.Object, mockProductRepo.Object, config);

        // Act
        var result = await controller.GetMonthlySalesReport();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var report = ((IEnumerable<MonthlySalesReportItemDTO>)okResult.Value!).ToList();
        Assert.Equal(2, report.Count);
    }

    [Fact]
    public async Task GetMonthlySalesReport_Should_Aggregate_By_Month()
    {
        // Arrange
        var mockSaleRepo = new Mock<ISaleRepository>();
        var mockProductRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration();

        var monthlyReport = new List<MonthlySalesReportItemDTO>
        {
            new() { Month = System.DateTime.Parse("2026-04-01"), TotalAmount = 100000 }
        };

        mockSaleRepo.Setup(r => r.GetMonthlySalesReportAsync()).ReturnsAsync(monthlyReport);

        var controller = new ReportsController(mockSaleRepo.Object, mockProductRepo.Object, config);

        // Act
        var result = await controller.GetMonthlySalesReport();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var report = ((IEnumerable<MonthlySalesReportItemDTO>)okResult.Value!).ToList();
        Assert.Single(report);
        Assert.Equal(100000, report[0].TotalAmount);
    }

    [Fact]
    public async Task GetMonthlySalesReport_Should_Handle_Multiple_Years()
    {
        // Arrange
        var mockSaleRepo = new Mock<ISaleRepository>();
        var mockProductRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration();

        var monthlyReport = new List<MonthlySalesReportItemDTO>
        {
            new() { Month = System.DateTime.Parse("2025-12-01"), TotalAmount = 30000 },
            new() { Month = System.DateTime.Parse("2026-01-01"), TotalAmount = 35000 }
        };

        mockSaleRepo.Setup(r => r.GetMonthlySalesReportAsync()).ReturnsAsync(monthlyReport);

        var controller = new ReportsController(mockSaleRepo.Object, mockProductRepo.Object, config);

        // Act
        var result = await controller.GetMonthlySalesReport();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var report = ((IEnumerable<MonthlySalesReportItemDTO>)okResult.Value!).ToList();
        Assert.Equal(2, report.Count);
    }

    #endregion

    #region Story S3-US-12: Low Stock Report

    [Fact]
    public async Task GetLowStockReport_Should_Filter_By_Threshold()
    {
        // Arrange
        var mockSaleRepo = new Mock<ISaleRepository>();
        var mockProductRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration(10);

        var products = new List<Product>
        {
            new() { Id = 1, Name = "Low", Quantity = 5, Category = "T", Price = 1000, SKU = "L1" },
            new() { Id = 2, Name = "Normal", Quantity = 20, Category = "T", Price = 1000, SKU = "N1" }
        };

        mockProductRepo.Setup(r => r.GetAllProducts()).ReturnsAsync(products);

        var controller = new ReportsController(mockSaleRepo.Object, mockProductRepo.Object, config);

        // Act
        var result = await controller.GetLowStockReport();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var lowStock = ((IEnumerable<InventoryProductResponseDTO>)okResult.Value!).ToList();
        Assert.Single(lowStock);
        Assert.Equal("Low", lowStock[0].Name);
    }

    [Fact]
    public async Task GetLowStockReport_Should_Include_Product_Details()
    {
        // Arrange
        var mockSaleRepo = new Mock<ISaleRepository>();
        var mockProductRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration(10);

        var products = new List<Product>
        {
            new() 
            { 
                Id = 1, 
                Name = "Low Stock Item", 
                Quantity = 5, 
                Category = "Tools", 
                Price = 2500, 
                SKU = "LSI-001",
                SupplierId = 3
            }
        };

        mockProductRepo.Setup(r => r.GetAllProducts()).ReturnsAsync(products);

        var controller = new ReportsController(mockSaleRepo.Object, mockProductRepo.Object, config);

        // Act
        var result = await controller.GetLowStockReport();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var lowStock = ((IEnumerable<InventoryProductResponseDTO>)okResult.Value!).ToList();
        var item = lowStock[0];

        Assert.Equal(1, item.Id);
        Assert.Equal("Low Stock Item", item.Name);
        Assert.Equal(5, item.Quantity);
        Assert.Equal("Tools", item.Category);
        Assert.Equal(2500, item.Price);
        Assert.Equal("LSI-001", item.SKU);
        Assert.Equal(3, item.SupplierId);
    }

    [Fact]
    public async Task GetLowStockReport_Should_Return_Empty_When_No_Low_Stock()
    {
        // Arrange
        var mockSaleRepo = new Mock<ISaleRepository>();
        var mockProductRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration(10);

        var products = new List<Product>
        {
            new() { Id = 1, Name = "Normal", Quantity = 50, Category = "T", Price = 1000, SKU = "N1" }
        };

        mockProductRepo.Setup(r => r.GetAllProducts()).ReturnsAsync(products);

        var controller = new ReportsController(mockSaleRepo.Object, mockProductRepo.Object, config);

        // Act
        var result = await controller.GetLowStockReport();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var lowStock = ((IEnumerable<InventoryProductResponseDTO>)okResult.Value!).ToList();
        Assert.Empty(lowStock);
    }

    #endregion

    #region Story S3-US-13: Export Report (CSV/PDF)

    [Fact]
    public async Task ExportReport_Should_Generate_CSV_For_Daily()
    {
        // Arrange
        var mockSaleRepo = new Mock<ISaleRepository>();
        var mockProductRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration();

        var dailyReport = new List<DailySalesReportItemDTO>
        {
            new() { Date = System.DateTime.Parse("2026-04-01"), TotalAmount = 5000 },
            new() { Date = System.DateTime.Parse("2026-04-02"), TotalAmount = 3500 }
        };

        mockSaleRepo.Setup(r => r.GetDailySalesReportAsync()).ReturnsAsync(dailyReport);

        var controller = new ReportsController(mockSaleRepo.Object, mockProductRepo.Object, config);

        // Act
        var result = await controller.ExportReport("daily");

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Contains("daily-sales-report", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task ExportReport_Should_Generate_CSV_For_Monthly()
    {
        // Arrange
        var mockSaleRepo = new Mock<ISaleRepository>();
        var mockProductRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration();

        var monthlyReport = new List<MonthlySalesReportItemDTO>
        {
            new() { Month = System.DateTime.Parse("2026-04-01"), TotalAmount = 50000 }
        };

        mockSaleRepo.Setup(r => r.GetMonthlySalesReportAsync()).ReturnsAsync(monthlyReport);

        var controller = new ReportsController(mockSaleRepo.Object, mockProductRepo.Object, config);

        // Act
        var result = await controller.ExportReport("monthly");

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Contains("monthly-sales-report", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task ExportReport_Should_Generate_CSV_For_Low_Stock()
    {
        // Arrange
        var mockSaleRepo = new Mock<ISaleRepository>();
        var mockProductRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration(10);

        var products = new List<Product>
        {
            new() { Id = 1, Name = "Low Item", Quantity = 5, Category = "T", Price = 1000, SKU = "L1" }
        };

        mockProductRepo.Setup(r => r.GetAllProducts()).ReturnsAsync(products);

        var controller = new ReportsController(mockSaleRepo.Object, mockProductRepo.Object, config);

        // Act
        var result = await controller.ExportReport("low-stock");

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Contains("low-stock-report", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task ExportReport_Should_Return_BadRequest_For_Invalid_Type()
    {
        // Arrange
        var mockSaleRepo = new Mock<ISaleRepository>();
        var mockProductRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration();

        var controller = new ReportsController(mockSaleRepo.Object, mockProductRepo.Object, config);

        // Act
        var result = await controller.ExportReport("invalid_type");

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task ExportReport_Should_Escape_Special_Characters_In_CSV()
    {
        // Arrange
        var mockSaleRepo = new Mock<ISaleRepository>();
        var mockProductRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration(10);

        var products = new List<Product>
        {
            new() 
            { 
                Id = 1, 
                Name = "Product, with \"quote\"", 
                Quantity = 5, 
                Category = "Tools/Hardware", 
                Price = 1000, 
                SKU = "P-001"
            }
        };

        mockProductRepo.Setup(r => r.GetAllProducts()).ReturnsAsync(products);

        var controller = new ReportsController(mockSaleRepo.Object, mockProductRepo.Object, config);

        // Act
        var result = await controller.ExportReport("low-stock");

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        var csvContent = Encoding.UTF8.GetString(fileResult.FileContents);
        
        // CSV escaping: quotes should be doubled and field should be wrapped
        Assert.Contains("\"Product, with \"\"quote\"\"\"", csvContent);
    }

    #endregion
}
