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

public class ProductControllerTests
{
    [Fact]
    public async Task GetProducts_Should_Return_Ok()
    {
        var mockRepo = new Mock<IProductRepository>();

        mockRepo.Setup(repo => repo.GetAllProducts())
                .Returns(Task.FromResult(new List<Product>
                {
                    new Product
                    {
                        Id = 1,
                        Name = "Hammer",
                        Price = 1500,
                        Quantity = 10
                    }
                }));

        var controller = new ProductController(mockRepo.Object);

        var result = await controller.GetProducts();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task AddProduct_Should_Return_Created()
    {
        var mockRepo = new Mock<IProductRepository>();

        var dto = new ProductCreateDTO
        {
            Name = "Hammer",
            SKU = "HM-100",
            Price = 1500,
            Quantity = 10,
            Category = "Hand Tools"
        };

        mockRepo.Setup(repo => repo.AddProduct(dto))
                .Returns(Task.FromResult(1));

        var controller = new ProductController(mockRepo.Object);

        var result = await controller.AddProduct(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
    }

    [Fact]
    public async Task AddProduct_Should_Return_BadRequest_When_SupplierId_Is_Invalid()
    {
        var mockRepo = new Mock<IProductRepository>();
        var controller = new ProductController(mockRepo.Object);

        var dto = new ProductCreateDTO
        {
            Name = "Hammer",
            SKU = "HM-100",
            Price = 1500,
            Quantity = 10,
            Category = "Hand Tools",
            SupplierId = 0
        };

        var result = await controller.AddProduct(dto);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_Should_Return_Ok_When_Deleted()
    {
        var mockRepo = new Mock<IProductRepository>();

        mockRepo.Setup(repo => repo.DeleteProduct(1))
                .Returns(Task.FromResult(true));

        var controller = new ProductController(mockRepo.Object);

        var result = await controller.DeleteProduct(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_Should_Return_NotFound_When_Not_Updated()
    {
        var mockRepo = new Mock<IProductRepository>();

        var dto = new ProductUpdateDTO();

        mockRepo.Setup(repo => repo.UpdateProduct(1, dto))
                .Returns(Task.FromResult(false));

        var controller = new ProductController(mockRepo.Object);

        var result = await controller.UpdateProduct(1, dto);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_Should_Return_BadRequest_When_SupplierId_Is_Invalid()
    {
        var mockRepo = new Mock<IProductRepository>();
        var controller = new ProductController(mockRepo.Object);

        var dto = new ProductUpdateDTO
        {
            SupplierId = -10
        };

        var result = await controller.UpdateProduct(1, dto);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task GetInventoryProducts_Should_Set_LowStock_Flag_Based_On_Quantity()
    {
        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(repo => repo.GetAllProducts())
                .Returns(Task.FromResult(new List<Product>
                {
                    new Product
                    {
                        Id = 1,
                        Name = "Hammer",
                        SKU = "HM-100",
                        Price = 1500,
                        Quantity = 5,
                        Category = "Hand Tools"
                    },
                    new Product
                    {
                        Id = 2,
                        Name = "Screwdriver",
                        SKU = "SD-200",
                        Price = 950,
                        Quantity = 30,
                        Category = "Hand Tools"
                    }
                }));

        var controller = new ProductController(mockRepo.Object);

        var result = await controller.GetInventoryProducts();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IEnumerable<InventoryProductResponseDTO>>(okResult.Value);
        var products = payload.ToList();

        Assert.Equal(2, products.Count);
        Assert.True(products.First(p => p.Id == 1).IsLowStock);
        Assert.False(products.First(p => p.Id == 2).IsLowStock);
    }

    [Fact]
    public async Task GetLowStockProducts_Should_Filter_By_Configured_Threshold()
    {
        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(repo => repo.GetLowStockProducts(10))
                .Returns(Task.FromResult(new List<Product>
                {
                    new Product
                    {
                        Id = 1,
                        Name = "Hammer",
                        SKU = "HM-100",
                        Price = 1500,
                        Quantity = 9,
                        Category = "Hand Tools"
                    }
                }));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LOW_STOCK_THRESHOLD"] = "10"
            })
            .Build();

        var controller = new ProductController(mockRepo.Object, config);

        var result = await controller.GetLowStockProducts();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IEnumerable<InventoryProductResponseDTO>>(okResult.Value);
        var products = payload.ToList();

        Assert.Single(products);
        Assert.Equal(1, products[0].Id);
        Assert.True(products[0].IsLowStock);
    }

    [Fact]
    public async Task UpdateProductStock_Should_Return_BadRequest_When_Quantity_Is_Negative()
    {
        var mockRepo = new Mock<IProductRepository>();
        var controller = new ProductController(mockRepo.Object);

        var dto = new ProductStockUpdateDTO
        {
            Quantity = -1,
            Reason = "Manual correction"
        };

        var result = await controller.UpdateProductStock(1, dto);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task UpdateProductStock_Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        var mockRepo = new Mock<IProductRepository>();
        var dto = new ProductStockUpdateDTO { Quantity = 20 };

        mockRepo.Setup(repo => repo.UpdateProductStock(1, dto))
                .Returns(Task.FromResult(false));

        var controller = new ProductController(mockRepo.Object);

        var result = await controller.UpdateProductStock(1, dto);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public async Task UpdateProductStock_Should_Return_Ok_When_Updated()
    {
        var mockRepo = new Mock<IProductRepository>();
        var dto = new ProductStockUpdateDTO { Quantity = 15, Reason = "Restock" };

        mockRepo.Setup(repo => repo.UpdateProductStock(1, dto))
                .Returns(Task.FromResult(true));

        var controller = new ProductController(mockRepo.Object);

        var result = await controller.UpdateProductStock(1, dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }
}
