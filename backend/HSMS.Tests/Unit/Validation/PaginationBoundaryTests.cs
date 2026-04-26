using HSMS.API.Controllers;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using HSMS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HSMS.Tests;

public class PaginationBoundaryTests
{
    [Fact]
    public async Task SearchProducts_Should_Reject_Empty_Query_String()
    {
        var repository = new Mock<IProductRepository>();
        var controller = new ProductController(repository.Object);

        var result = await controller.SearchProducts(string.Empty, 20);

        Assert.IsType<BadRequestObjectResult>(result);
        repository.Verify(repo => repo.SearchProducts(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SearchProducts_Should_Reject_Whitespace_Only_Query()
    {
        var repository = new Mock<IProductRepository>();
        var controller = new ProductController(repository.Object);

        var result = await controller.SearchProducts("   ", 20);

        Assert.IsType<BadRequestObjectResult>(result);
        repository.Verify(repo => repo.SearchProducts(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(1000)]
    public async Task SearchProducts_Should_Forward_Trimmed_Query_And_Limit(int limit)
    {
        var repository = new Mock<IProductRepository>();
        repository.Setup(repo => repo.SearchProducts("hammer", limit))
            .ReturnsAsync(
            [
                new Product { Id = 1, Name = "Hammer", SKU = "HAM-1", Price = 100m, Quantity = 10, Category = "Tools" }
            ]);

        var controller = new ProductController(repository.Object);

        var result = await controller.SearchProducts("  hammer  ", limit);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var products = Assert.IsAssignableFrom<List<Product>>(okResult.Value);
        Assert.Single(products);
        repository.Verify(repo => repo.SearchProducts("hammer", limit), Times.Once);
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(0)]
    public async Task GetHistory_Should_Reject_Non_Positive_Limit(int invalidLimit)
    {
        var repository = new Mock<ISaleRepository>();
        var controller = new SalesController(repository.Object);

        var result = await controller.GetHistory(null, null, null, invalidLimit);

        Assert.IsType<BadRequestObjectResult>(result);
        repository.Verify(repo => repo.GetSalesHistoryAsync(It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetHistory_Should_Use_Default_Limit_Of_100()
    {
        var repository = new Mock<ISaleRepository>();
        repository.Setup(repo => repo.GetSalesHistoryAsync(null, null, null, 100))
            .ReturnsAsync([]);

        var controller = new SalesController(repository.Object);

        var result = await controller.GetHistory(null, null, null);

        Assert.IsType<OkObjectResult>(result);
        repository.Verify(repo => repo.GetSalesHistoryAsync(null, null, null, 100), Times.Once);
    }

    [Fact]
    public async Task GetHistory_Should_Return_At_Most_Requested_Limit()
    {
        const int limit = 5;

        var repository = new Mock<ISaleRepository>();
        repository.Setup(repo => repo.GetSalesHistoryAsync(null, null, null, limit))
            .ReturnsAsync(Enumerable.Range(1, limit)
                .Select(i => new SaleHistoryItemDTO
                {
                    SaleId = i,
                    SoldAt = DateTime.UtcNow,
                    TotalAmount = 100m,
                    SoldBy = "cashier1",
                    ItemCount = 1
                })
                .ToList());

        var controller = new SalesController(repository.Object);

        var result = await controller.GetHistory(null, null, null, limit);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var history = Assert.IsAssignableFrom<List<SaleHistoryItemDTO>>(okResult.Value);
        Assert.Equal(limit, history.Count);
    }

    [Fact]
    public async Task GetSalesAnalytics_Should_Reject_Invalid_Date_Range()
    {
        var saleRepository = new Mock<ISaleRepository>();
        var productRepository = new Mock<IProductRepository>();
        var controller = new ReportsController(saleRepository.Object, productRepository.Object);

        var fromDate = new DateTime(2024, 1, 10);
        var toDate = new DateTime(2024, 1, 5);

        var result = await controller.GetSalesAnalytics(fromDate, toDate, null, null);

        Assert.IsType<BadRequestObjectResult>(result);
        saleRepository.Verify(repo => repo.GetSalesAnalyticsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public async Task GetSalesAnalytics_Should_Accept_Same_Start_And_End_Date()
    {
        var saleRepository = new Mock<ISaleRepository>();
        var productRepository = new Mock<IProductRepository>();
        saleRepository.Setup(repo => repo.GetSalesAnalyticsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, null, It.IsAny<decimal>()))
            .ReturnsAsync(new SalesAnalyticsResponseDTO());

        var controller = new ReportsController(saleRepository.Object, productRepository.Object);
        var sameDate = new DateTime(2024, 1, 15);

        var result = await controller.GetSalesAnalytics(sameDate, sameDate, null, null);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetSalesAnalytics_Should_Handle_Null_Date_Range()
    {
        var saleRepository = new Mock<ISaleRepository>();
        var productRepository = new Mock<IProductRepository>();
        saleRepository.Setup(repo => repo.GetSalesAnalyticsAsync(null, null, null, null, It.IsAny<decimal>()))
            .ReturnsAsync(new SalesAnalyticsResponseDTO());

        var controller = new ReportsController(saleRepository.Object, productRepository.Object);

        var result = await controller.GetSalesAnalytics(null, null, null, null);

        Assert.IsType<OkObjectResult>(result);
    }
}
