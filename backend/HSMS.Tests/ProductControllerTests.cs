using Xunit;
using Moq;
using HSMS.API.Controllers;
using HSMS.Application.Interfaces;
using HSMS.Application.DTOs;
using HSMS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
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
            Price = 1500,
            Quantity = 10
        };

        mockRepo.Setup(repo => repo.AddProduct(dto))
                .Returns(Task.FromResult(1));

        var controller = new ProductController(mockRepo.Object);

        var result = await controller.AddProduct(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
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
}