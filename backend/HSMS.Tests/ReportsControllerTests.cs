using HSMS.API.Controllers;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using HSMS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Moq;

namespace HSMS.Tests;

public class ReportsControllerTests
{
    private static ReportsController CreateController(
        Mock<ISaleRepository> saleRepo,
        Mock<IProductRepository>? productRepo = null,
        IConfiguration? config = null,
        int? currentUserId = null)
    {
        if (productRepo == null)
        {
            productRepo = new Mock<IProductRepository>();
            productRepo.Setup(repo => repo.GetAllProducts()).ReturnsAsync([]);
        }
        
        var controller = new ReportsController(saleRepo.Object, productRepo.Object, config);
        
        // Set up mock User context if currentUserId is provided
        if (currentUserId.HasValue)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, currentUserId.Value.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestScheme");
            var principal = new ClaimsPrincipal(identity);
            
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(ctx => ctx.User).Returns(principal);
            
            var controllerContext = new ControllerContext { HttpContext = mockHttpContext.Object };
            controller.ControllerContext = controllerContext;
        }
        
        return controller;
    }

    [Fact]
    public async Task GetDailySalesReport_Should_Return_Ok_With_Report_Data()
    {
        var saleRepo = new Mock<ISaleRepository>();
        saleRepo.Setup(repo => repo.GetDailySalesReportAsync())
            .ReturnsAsync(
            [
                new DailySalesReportItemDTO
                {
                    Date = new DateTime(2026, 4, 3),
                    TotalAmount = 15000m
                },
                new DailySalesReportItemDTO
                {
                    Date = new DateTime(2026, 4, 2),
                    TotalAmount = 9800m
                }
            ]);

        var controller = CreateController(saleRepo);
        var result = await controller.GetDailySalesReport();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task GetMonthlySalesReport_Should_Return_Ok_With_Report_Data()
    {
        var saleRepo = new Mock<ISaleRepository>();
        saleRepo.Setup(repo => repo.GetMonthlySalesReportAsync())
            .ReturnsAsync(
            [
                new MonthlySalesReportItemDTO
                {
                    Month = new DateTime(2026, 4, 1),
                    TotalAmount = 320000m
                },
                new MonthlySalesReportItemDTO
                {
                    Month = new DateTime(2026, 3, 1),
                    TotalAmount = 287500m
                }
            ]);

        var controller = CreateController(saleRepo);
        var result = await controller.GetMonthlySalesReport();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task GetLowStockReport_Should_Filter_By_Configured_Threshold()
    {
        var saleRepo = new Mock<ISaleRepository>();
        var productRepo = new Mock<IProductRepository>();

        productRepo.Setup(repo => repo.GetAllProducts())
            .ReturnsAsync(
            [
                new Product
                {
                    Id = 1,
                    Name = "Hammer",
                    SKU = "HM-1",
                    Quantity = 9,
                    Category = "Tools",
                    Price = 1500m
                },
                new Product
                {
                    Id = 2,
                    Name = "Drill",
                    SKU = "DR-1",
                    Quantity = 10,
                    Category = "Power Tools",
                    Price = 12000m
                }
            ]);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LOW_STOCK_THRESHOLD"] = "10"
            })
            .Build();

        var controller = CreateController(saleRepo, productRepo, config);
        var result = await controller.GetLowStockReport();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);

        var payload = Assert.IsAssignableFrom<IEnumerable<InventoryProductResponseDTO>>(ok.Value);
        var products = payload.ToList();

        Assert.Single(products);
        Assert.Equal(1, products[0].Id);
        Assert.True(products[0].IsLowStock);
    }

    [Fact]
    public async Task ExportReport_Should_Return_Csv_File_When_Type_Is_Daily()
    {
        var saleRepo = new Mock<ISaleRepository>();
        var productRepo = new Mock<IProductRepository>();

        saleRepo.Setup(repo => repo.GetDailySalesReportAsync())
            .ReturnsAsync(
            [
                new DailySalesReportItemDTO
                {
                    Date = new DateTime(2026, 4, 3),
                    TotalAmount = 15000m
                }
            ]);

        var controller = new ReportsController(saleRepo.Object, productRepo.Object);
        var result = await controller.ExportReport("daily");

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", file.ContentType);
        Assert.Contains("daily-sales-report-", file.FileDownloadName);
        Assert.EndsWith(".csv", file.FileDownloadName);
    }

    [Fact]
    public async Task ExportReport_Should_Include_Monthly_Csv_Content()
    {
        var saleRepo = new Mock<ISaleRepository>();
        var productRepo = new Mock<IProductRepository>();

        saleRepo.Setup(repo => repo.GetMonthlySalesReportAsync())
            .ReturnsAsync(
            [
                new MonthlySalesReportItemDTO
                {
                    Month = new DateTime(2026, 4, 1),
                    TotalAmount = 320000m
                }
            ]);

        var controller = new ReportsController(saleRepo.Object, productRepo.Object);
        var result = await controller.ExportReport("monthly");

        var file = Assert.IsType<FileContentResult>(result);
        string content = System.Text.Encoding.UTF8.GetString(file.FileContents);

        Assert.Contains("Month,TotalSales", content);
        Assert.Contains("2026-04,320000.00", content);
    }

    [Fact]
    public async Task ExportReport_Should_Escape_Csv_Fields_For_LowStock_Report()
    {
        var saleRepo = new Mock<ISaleRepository>();
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(repo => repo.GetAllProducts())
            .ReturnsAsync(
            [
                new Product
                {
                    Id = 1,
                    Name = "Hammer, Heavy Duty",
                    SKU = "HM-1",
                    Quantity = 3,
                    Category = "Hand \"Tools\"",
                    Price = 1500m
                }
            ]);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LOW_STOCK_THRESHOLD"] = "10"
            })
            .Build();

        var controller = new ReportsController(saleRepo.Object, productRepo.Object, config);
        var result = await controller.ExportReport("low-stock");

        var file = Assert.IsType<FileContentResult>(result);
        string content = System.Text.Encoding.UTF8.GetString(file.FileContents);

        Assert.Contains("Product,SKU,Category,Quantity,Price", content);
        Assert.Contains("\"Hammer, Heavy Duty\",HM-1,\"Hand \"\"Tools\"\"\",3,1500.00", content);
    }

    [Fact]
    public async Task ExportReport_Should_Return_BadRequest_When_Type_Is_Unsupported()
    {
        var saleRepo = new Mock<ISaleRepository>();
        var productRepo = new Mock<IProductRepository>();

        var controller = CreateController(saleRepo, productRepo, currentUserId: 1); // Provide User context
        var result = await controller.ExportReport("unsupported"); // Use an unsupported type

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }
}
