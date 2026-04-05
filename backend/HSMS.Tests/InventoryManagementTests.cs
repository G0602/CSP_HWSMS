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
using System.Threading.Tasks;

namespace HSMS.Tests;

/// <summary>
/// Sprint 3 - EPIC 3.1: Inventory Management Unit Tests
/// Tests for stories: S3-US-01, S3-US-02, S3-US-03
/// </summary>
public class InventoryManagementTests
{
    private static IConfiguration CreateConfiguration(int? lowStockThreshold = 10)
    {
        var config = new Dictionary<string, string?>
        {
            { "LOW_STOCK_THRESHOLD", lowStockThreshold?.ToString() ?? "10" }
        };
        return new ConfigurationBuilder().AddInMemoryCollection(config).Build();
    }

    #region Story S3-US-01: View Inventory with Stock Status

    [Fact]
    public async Task GetInventoryProducts_Should_Return_All_Products_With_Stock_Status()
    {
        // Arrange
        var mockRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration();

        var products = new List<Product>
        {
            new() { Id = 1, Name = "Hammer", Quantity = 5, Category = "Hand Tools", Price = 1500, SKU = "HM-100" },
            new() { Id = 2, Name = "Screwdriver", Quantity = 25, Category = "Hand Tools", Price = 800, SKU = "SD-100" }
        };

        mockRepo.Setup(r => r.GetAllProducts()).ReturnsAsync(products);
        var controller = new ProductController(mockRepo.Object, config);

        // Act
        var result = await controller.GetInventoryProducts();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var inventory = ((IEnumerable<InventoryProductResponseDTO>)okResult.Value!).ToList();
        Assert.Equal(2, inventory.Count);
        Assert.Equal("Hammer", inventory[0].Name);
    }

    [Fact]
    public async Task GetInventoryProducts_Should_Include_Quantity_Category_Price()
    {
        // Arrange
        var mockRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration();

        var product = new Product
        {
            Id = 1,
            Name = "Product",
            Quantity = 15,
            Category = "Tools",
            Price = 2000,
            SKU = "P-001"
        };

        mockRepo.Setup(r => r.GetAllProducts()).ReturnsAsync(new List<Product> { product });
        var controller = new ProductController(mockRepo.Object, config);

        // Act
        var result = await controller.GetInventoryProducts();
        var okResult = Assert.IsType<OkObjectResult>(result);
        var inventory = ((IEnumerable<InventoryProductResponseDTO>)okResult.Value!).First();

        // Assert
        Assert.Equal(15, inventory.Quantity);
        Assert.Equal("Tools", inventory.Category);
        Assert.Equal(2000, inventory.Price);
    }

    [Fact]
    public async Task GetInventoryProducts_Should_Mark_Low_Stock_Items_Correctly()
    {
        // Arrange
        var mockRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration(10);

        var products = new List<Product>
        {
            new() { Id = 1, Name = "Low", Quantity = 5, Category = "T", Price = 1000, SKU = "L1" },
            new() { Id = 2, Name = "Normal", Quantity = 20, Category = "T", Price = 1000, SKU = "N1" },
            new() { Id = 3, Name = "AtThreshold", Quantity = 10, Category = "T", Price = 1000, SKU = "AT1" }
        };

        mockRepo.Setup(r => r.GetAllProducts()).ReturnsAsync(products);
        var controller = new ProductController(mockRepo.Object, config);

        // Act
        var result = await controller.GetInventoryProducts();
        var okResult = Assert.IsType<OkObjectResult>(result);
        var inventory = ((IEnumerable<InventoryProductResponseDTO>)okResult.Value!).ToList();

        // Assert
        Assert.True(inventory.First(p => p.Id == 1).IsLowStock);
        Assert.False(inventory.First(p => p.Id == 2).IsLowStock);
        Assert.False(inventory.First(p => p.Id == 3).IsLowStock);
    }

    [Fact]
    public async Task GetInventoryProducts_Should_Include_Supplier_Information()
    {
        // Arrange
        var mockRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration();

        var products = new List<Product>
        {
            new() { Id = 1, Name = "With Supplier", Quantity = 15, Category = "T", Price = 1000, SKU = "WS1", SupplierId = 5 },
            new() { Id = 2, Name = "No Supplier", Quantity = 20, Category = "T", Price = 1000, SKU = "NS1", SupplierId = null }
        };

        mockRepo.Setup(r => r.GetAllProducts()).ReturnsAsync(products);
        var controller = new ProductController(mockRepo.Object, config);

        // Act
        var result = await controller.GetInventoryProducts();
        var okResult = Assert.IsType<OkObjectResult>(result);
        var inventory = ((IEnumerable<InventoryProductResponseDTO>)okResult.Value!).ToList();

        // Assert
        Assert.Equal(5, inventory.First(p => p.Id == 1).SupplierId);
        Assert.Null(inventory.First(p => p.Id == 2).SupplierId);
    }

    #endregion

    #region Story S3-US-02: Low Stock Alerts

    [Fact]
    public async Task GetLowStockProducts_Should_Filter_Products_Below_Threshold()
    {
        // Arrange
        var mockRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration(10);

        var products = new List<Product>
        {
            new() { Id = 1, Name = "Low1", Quantity = 3, Category = "T", Price = 1000, SKU = "L1" },
            new() { Id = 2, Name = "Low2", Quantity = 8, Category = "T", Price = 1000, SKU = "L2" },
            new() { Id = 3, Name = "Normal", Quantity = 15, Category = "T", Price = 1000, SKU = "N1" },
            new() { Id = 4, Name = "High", Quantity = 50, Category = "T", Price = 1000, SKU = "H1" }
        };

        mockRepo.Setup(r => r.GetAllProducts()).ReturnsAsync(products);
        var controller = new ProductController(mockRepo.Object, config);

        // Act
        var result = await controller.GetLowStockProducts();
        var okResult = Assert.IsType<OkObjectResult>(result);
        var lowStockProducts = ((IEnumerable<InventoryProductResponseDTO>)okResult.Value!).ToList();

        // Assert
        Assert.Equal(2, lowStockProducts.Count);
        Assert.All(lowStockProducts, p => Assert.True(p.IsLowStock));
    }

    [Theory]
    [InlineData(5, 4)]     // qty < threshold = low stock
    [InlineData(5, 5)]     // qty = threshold = NOT low stock
    [InlineData(10, 9)]    // qty < threshold = low stock
    [InlineData(10, 10)]   // qty = threshold = NOT low stock
    public async Task GetLowStockProducts_Should_Respect_Threshold_Configuration(int threshold, int quantity)
    {
        // Arrange
        var mockRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration(threshold);

        var products = new List<Product>
        {
            new() { Id = 1, Name = "Test", Quantity = quantity, Category = "T", Price = 1000, SKU = "T1" }
        };

        mockRepo.Setup(r => r.GetAllProducts()).ReturnsAsync(products);
        var controller = new ProductController(mockRepo.Object, config);

        // Act
        var result = await controller.GetLowStockProducts();
        var okResult = Assert.IsType<OkObjectResult>(result);
        var lowStock = ((IEnumerable<InventoryProductResponseDTO>)okResult.Value!).ToList();

        // Assert
        bool expectedLowStock = quantity < threshold;
        Assert.Equal(expectedLowStock ? 1 : 0, lowStock.Count);
    }

    [Fact]
    public async Task GetLowStockProducts_Should_Mark_All_Returned_Items_As_LowStock()
    {
        // Arrange
        var mockRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration(15);

        var products = new List<Product>
        {
            new() { Id = 1, Name = "Low1", Quantity = 5, Category = "T", Price = 1000, SKU = "L1" },
            new() { Id = 2, Name = "Low2", Quantity = 10, Category = "T", Price = 1000, SKU = "L2" }
        };

        mockRepo.Setup(r => r.GetAllProducts()).ReturnsAsync(products);
        var controller = new ProductController(mockRepo.Object, config);

        // Act
        var result = await controller.GetLowStockProducts();
        var okResult = Assert.IsType<OkObjectResult>(result);
        var lowStock = ((IEnumerable<InventoryProductResponseDTO>)okResult.Value!).ToList();

        // Assert
        Assert.All(lowStock, p => Assert.True(p.IsLowStock));
    }

    [Fact]
    public async Task GetLowStockProducts_Should_Return_Empty_When_No_Low_Stock_Items()
    {
        // Arrange
        var mockRepo = new Mock<IProductRepository>();
        var config = CreateConfiguration(10);

        var products = new List<Product>
        {
            new() { Id = 1, Name = "Normal1", Quantity = 20, Category = "T", Price = 1000, SKU = "N1" },
            new() { Id = 2, Name = "Normal2", Quantity = 30, Category = "T", Price = 1000, SKU = "N2" }
        };

        mockRepo.Setup(r => r.GetAllProducts()).ReturnsAsync(products);
        var controller = new ProductController(mockRepo.Object, config);

        // Act
        var result = await controller.GetLowStockProducts();
        var okResult = Assert.IsType<OkObjectResult>(result);
        var lowStock = ((IEnumerable<InventoryProductResponseDTO>)okResult.Value!).ToList();

        // Assert
        Assert.Empty(lowStock);
    }

    #endregion

    #region Story S3-US-03: Manual Stock Update

    [Fact]
    public async Task UpdateProductStock_Should_Return_Ok_When_Valid()
    {
        // Arrange
        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(r => r.UpdateProductStock(1, It.IsAny<ProductStockUpdateDTO>())).ReturnsAsync(true);

        var controller = new ProductController(mockRepo.Object);
        var dto = new ProductStockUpdateDTO { Quantity = 25, Reason = "Stock correction" };

        // Act
        var result = await controller.UpdateProductStock(1, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task UpdateProductStock_Should_Return_BadRequest_When_Quantity_Is_Negative()
    {
        // Arrange
        var mockRepo = new Mock<IProductRepository>();
        var controller = new ProductController(mockRepo.Object);
        var dto = new ProductStockUpdateDTO { Quantity = -5, Reason = "Invalid" };

        // Act
        var result = await controller.UpdateProductStock(1, dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task UpdateProductStock_Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        // Arrange
        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(r => r.UpdateProductStock(999, It.IsAny<ProductStockUpdateDTO>())).ReturnsAsync(false);

        var controller = new ProductController(mockRepo.Object);
        var dto = new ProductStockUpdateDTO { Quantity = 10 };

        // Act
        var result = await controller.UpdateProductStock(999, dto);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(9999)]
    public async Task UpdateProductStock_Should_Accept_Valid_Quantities(int quantity)
    {
        // Arrange
        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(r => r.UpdateProductStock(1, It.IsAny<ProductStockUpdateDTO>())).ReturnsAsync(true);

        var controller = new ProductController(mockRepo.Object);
        var dto = new ProductStockUpdateDTO { Quantity = quantity };

        // Act
        var result = await controller.UpdateProductStock(1, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    #endregion
}
