using HSMS.API.Controllers;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HSMS.Tests;

public class SuppliersControllerTests
{
    [Fact]
    public async Task AddSupplier_Should_Return_BadRequest_When_Name_Is_Empty()
    {
        var mockRepo = new Mock<ISupplierRepository>();
        var controller = new SuppliersController(mockRepo.Object);

        var result = await controller.AddSupplier(new SupplierCreateDTO { Name = "   " });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task AddSupplier_Should_Return_Created_When_Valid()
    {
        var mockRepo = new Mock<ISupplierRepository>();
        mockRepo.Setup(repo => repo.AddSupplierAsync(It.IsAny<SupplierCreateDTO>()))
            .ReturnsAsync(10);

        var controller = new SuppliersController(mockRepo.Object);

        var result = await controller.AddSupplier(new SupplierCreateDTO { Name = "ABC Suppliers" });

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, created.StatusCode);
    }
}
