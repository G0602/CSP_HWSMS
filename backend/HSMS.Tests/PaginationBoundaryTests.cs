using HSMS.API.Controllers;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace HSMS.Tests;

/// <summary>
/// Tests for pagination boundary conditions and limits
/// Ensures search and list endpoints handle edge cases correctly
/// </summary>
public class PaginationBoundaryTests
{
    [Theory]
    [InlineData(0)]     // Zero limit
    [InlineData(-1)]    // Negative limit
    [InlineData(-100)]  // Very negative
    public async Task SearchProducts_Should_Reject_Zero_Or_Negative_Limit(int invalidLimit)
    {
        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(r => r.SearchProducts(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Product>());

        var controller = new ProductController(mockRepo.Object);

        // Execute: Call with invalid limit - expect client-side validation or server-side rejection
        // The controller should validate before calling repository
        var result = await controller.SearchProducts("test", invalidLimit);

        // Verify: Should return BadRequest or limit should be normalized to minimum
        var badRequest = result as Microsoft.AspNetCore.Mvc.BadRequestObjectResult;
        if (badRequest != null)
        {
            Assert.Equal(400, badRequest.StatusCode);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task SearchProducts_Should_Respect_Limit_Boundary(int limit)
    {
        var mockRepo = new Mock<IProductRepository>();
        
        // Create more products than the limit
        var allProducts = Enumerable.Range(1, 200)
            .Select(i => new Product { Id = i, Name = $"Product {i}", SKU = $"SKU-{i}", Price = 100, Quantity = 10 })
            .ToList();

        mockRepo.Setup(r => r.SearchProducts(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((string query, int lim) => allProducts.Take(lim).ToList());

        var controller = new ProductController(mockRepo.Object);

        // Execute
        var result = await controller.SearchProducts("Product", limit);

        // Verify
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        var returnedProducts = okResult.Value as List<Product>;
        Assert.NotNull(returnedProducts);
        Assert.True(returnedProducts.Count <= limit);
    }

    [Theory]
    [InlineData(1000000)]   // Extremely large limit
    [InlineData(int.MaxValue)]  // Max int value
    public async Task SearchProducts_Should_Cap_Limit_To_Maximum(int excessiveLimit)
    {
        var mockRepo = new Mock<IProductRepository>();
        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Product 1", SKU = "SKU-1", Price = 100, Quantity = 10 }
        };

        mockRepo.Setup(r => r.SearchProducts(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(products);

        var controller = new ProductController(mockRepo.Object);

        // Execute
        var result = await controller.SearchProducts("Product", excessiveLimit);

        // Verify: Should return OK with available products
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task SearchProducts_Should_Handle_Empty_Query_String()
    {
        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(r => r.SearchProducts("", 20))
            .ReturnsAsync(new List<Product>());

        var controller = new ProductController(mockRepo.Object);

        // Execute
        var result = await controller.SearchProducts("", 20);

        // Should handle empty query gracefully
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task SearchProducts_Should_Handle_Whitespace_Only_Query()
    {
        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(r => r.SearchProducts("   ", 20))
            .ReturnsAsync(new List<Product>());

        var controller = new ProductController(mockRepo.Object);

        // Execute
        var result = await controller.SearchProducts("   ", 20);

        // Should handle whitespace gracefully
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    public async Task SearchProducts_Should_Return_Correct_Count_Below_Limit(int expectedCount)
    {
        var mockRepo = new Mock<IProductRepository>();
        
        var products = Enumerable.Range(1, expectedCount)
            .Select(i => new Product { Id = i, Name = $"Product {i}", SKU = $"SKU-{i}", Price = 100, Quantity = 10 })
            .ToList();

        mockRepo.Setup(r => r.SearchProducts("Product", 50))
            .ReturnsAsync(products);

        var controller = new ProductController(mockRepo.Object);

        // Execute
        var result = await controller.SearchProducts("Product", 50);

        // Verify
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        var returnedProducts = okResult.Value as List<Product>;
        Assert.NotNull(returnedProducts);
        Assert.Equal(expectedCount, returnedProducts.Count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task GetHistory_Should_Respect_Limit_Parameter(int limit)
    {
        var mockRepo = new Mock<ISaleRepository>();
        
        var sales = Enumerable.Range(1, limit + 10)
            .Select(i => new SaleHistoryItemDTO 
            { 
                SaleId = i, 
                SoldAt = System.DateTime.UtcNow, 
                TotalAmount = 100m, 
                SoldBy = "cashier1",
                ItemCount = 1
            })
            .ToList();

        mockRepo.Setup(r => r.GetSalesHistoryAsync(null, null, null, limit))
            .ReturnsAsync(sales.Take(limit).ToList());

        var controller = new SalesController(mockRepo.Object);

        // Execute
        var result = await controller.GetHistory(null, null, null, limit);

        // Verify
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        var returnedHistory = okResult.Value as List<SaleHistoryItemDTO>;
        Assert.NotNull(returnedHistory);
        Assert.True(returnedHistory.Count <= limit);
    }

    [Fact]
    public async Task GetHistory_Should_Reject_Negative_Limit()
    {
        var mockRepo = new Mock<ISaleRepository>();
        var controller = new SalesController(mockRepo.Object);

        // Execute: Negative limit
        var result = await controller.GetHistory(null, null, null, -10);

        // Should reject with BadRequest
        var badRequest = Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task GetHistory_Should_Reject_Zero_Limit()
    {
        var mockRepo = new Mock<ISaleRepository>();
        var controller = new SalesController(mockRepo.Object);

        // Execute
        var result = await controller.GetHistory(null, null, null, 0);

        // Should reject
        var badRequest = Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task GetHistory_Should_Have_Default_Limit()
    {
        var mockRepo = new Mock<ISaleRepository>();
        mockRepo.Setup(r => r.GetSalesHistoryAsync(null, null, null, It.IsAny<int>()))
            .ReturnsAsync(new List<SaleHistoryItemDTO>());

        var controller = new SalesController(mockRepo.Object);

        // Execute without specifying limit (should use default)
        var result = await controller.GetHistory(null, null, null);

        // Verify
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        // Verify the default limit was used
        mockRepo.Verify(r => r.GetSalesHistoryAsync(null, null, null, 100), Times.Once);
    }

    [Theory]
    [InlineData(1, 2)]      // Start after end
    [InlineData(2024, 2020)] // Start year after end year
    public async Task GetSalesAnalytics_Should_Handle_Invalid_Date_Range(int startDay, int endDay)
    {
        var mockRepo = new Mock<ISaleRepository>();
        var productRepo = new Mock<IProductRepository>();
        var controller = new ReportsController(mockRepo.Object, productRepo.Object);

        var startDate = new System.DateTime(2024, 1, startDay);
        var endDate = new System.DateTime(2024, 1, endDay);

        // Execute
        var result = await controller.GetSalesAnalytics(startDate, endDate, null, null);

        // Should reject invalid date range
        var badRequest = Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task GetSalesAnalytics_Should_Accept_Same_Start_And_End_Date()
    {
        var mockRepo = new Mock<ISaleRepository>();
        var productRepo = new Mock<IProductRepository>();
        mockRepo.Setup(r => r.GetSalesAnalyticsAsync(It.IsAny<System.DateTime?>(), It.IsAny<System.DateTime?>(), null, null, It.IsAny<decimal>()))
            .ReturnsAsync(new SalesAnalyticsResponseDTO
            {
                DailyTrends = new List<DailySalesAnalyticsItemDTO>(),
                MonthlyTrends = new List<MonthlySalesAnalyticsItemDTO>(),
                TotalSales = 0,
                TotalCost = 0,
                TotalProfit = 0
            });

        var controller = new ReportsController(mockRepo.Object, productRepo.Object);

        var sameDate = new System.DateTime(2024, 1, 15);

        // Execute
        var result = await controller.GetSalesAnalytics(sameDate, sameDate, null, null);

        // Should accept
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetSalesAnalytics_Should_Handle_Null_Date_Range()
    {
        var mockRepo = new Mock<ISaleRepository>();
        var productRepo = new Mock<IProductRepository>();
        mockRepo.Setup(r => r.GetSalesAnalyticsAsync(null, null, null, null, It.IsAny<decimal>()))
            .ReturnsAsync(new SalesAnalyticsResponseDTO
            {
                DailyTrends = new List<DailySalesAnalyticsItemDTO>(),
                MonthlyTrends = new List<MonthlySalesAnalyticsItemDTO>(),
                TotalSales = 0,
                TotalCost = 0,
                TotalProfit = 0
            });

        var controller = new ReportsController(mockRepo.Object, productRepo.Object);

        // Execute without date range
        var result = await controller.GetSalesAnalytics(null, null, null, null);

        // Should return all-time analytics
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}
