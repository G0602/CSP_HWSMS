using HSMS.API.Controllers;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using HSMS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HSMS.Tests;

public class ProductControllerEdgeCaseTests
{
    [Fact]
    public async Task AddProduct_Should_Return_BadRequest_When_Supplier_Does_Not_Exist()
    {
        var productRepository = new Mock<IProductRepository>();
        var supplierRepository = new Mock<ISupplierRepository>();
        supplierRepository.Setup(repo => repo.SupplierExistsAsync(99)).ReturnsAsync(false);
        var controller = new ProductController(productRepository.Object, supplierRepository: supplierRepository.Object);

        var result = await controller.AddProduct(new ProductCreateDTO
        {
            Name = "Hammer",
            SKU = "HM-99",
            Category = "Tools",
            Price = 1200m,
            Quantity = 10,
            SupplierId = 99
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Selected supplier was not found.", badRequest.Value);
        productRepository.Verify(repo => repo.AddProduct(It.IsAny<ProductCreateDTO>()), Times.Never);
    }

    [Fact]
    public async Task AddProduct_Should_Trim_Text_Fields_Before_Saving()
    {
        var productRepository = new Mock<IProductRepository>();
        productRepository.Setup(repo => repo.AddProduct(It.IsAny<ProductCreateDTO>())).ReturnsAsync(15);
        var controller = new ProductController(productRepository.Object);

        var result = await controller.AddProduct(new ProductCreateDTO
        {
            Name = "  Hammer  ",
            SKU = " HM-15 ",
            Category = " Tools ",
            Price = 1200m,
            Quantity = 10
        });

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ProductController.GetProductById), created.ActionName);
        productRepository.Verify(repo => repo.AddProduct(It.Is<ProductCreateDTO>(product =>
            product.Name == "Hammer" &&
            product.SKU == "HM-15" &&
            product.Category == "Tools")), Times.Once);
    }

    [Fact]
    public async Task UpdateProduct_Should_Return_BadRequest_When_SupplierId_Is_Invalid()
    {
        var controller = new ProductController(new Mock<IProductRepository>().Object);

        var result = await controller.UpdateProduct(1, new ProductUpdateDTO
        {
            Name = "Hammer",
            SKU = "HM-1",
            Category = "Tools",
            Price = 1200m,
            Quantity = 10,
            SupplierId = 0
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("SupplierId must be greater than zero.", badRequest.Value);
    }

    [Fact]
    public async Task SearchProducts_Should_Return_BadRequest_When_Query_Is_Blank()
    {
        var productRepository = new Mock<IProductRepository>();
        var controller = new ProductController(productRepository.Object);

        var result = await controller.SearchProducts("   ");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Search query is required.", badRequest.Value);
        productRepository.Verify(repo => repo.SearchProducts(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SearchProducts_Should_Trim_Query_And_Respect_Limit()
    {
        var productRepository = new Mock<IProductRepository>();
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Hammer", SKU = "HM-1", Category = "Tools", Price = 1200m, Quantity = 10 }
        };
        productRepository.Setup(repo => repo.SearchProducts("hammer", 5)).ReturnsAsync(products);
        var controller = new ProductController(productRepository.Object);

        var result = await controller.SearchProducts(" hammer ", 5);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(products, ok.Value);
    }
}
