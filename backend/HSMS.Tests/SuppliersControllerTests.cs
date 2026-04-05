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

    [Fact]
    public async Task UpdateSupplier_Should_Return_BadRequest_When_Name_Is_Empty()
    {
        var mockRepo = new Mock<ISupplierRepository>();
        var controller = new SuppliersController(mockRepo.Object);

        var result = await controller.UpdateSupplier(1, new SupplierUpdateDTO { Name = " " });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task UpdateSupplier_Should_Return_NotFound_When_Record_Does_Not_Exist()
    {
        var mockRepo = new Mock<ISupplierRepository>();
        mockRepo.Setup(repo => repo.UpdateSupplierAsync(1, It.IsAny<SupplierUpdateDTO>()))
            .ReturnsAsync(false);
        var controller = new SuppliersController(mockRepo.Object);

        var result = await controller.UpdateSupplier(1, new SupplierUpdateDTO { Name = "Supplier A" });

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public async Task UpdateSupplier_Should_Return_Ok_When_Record_Updated()
    {
        var mockRepo = new Mock<ISupplierRepository>();
        mockRepo.Setup(repo => repo.UpdateSupplierAsync(1, It.IsAny<SupplierUpdateDTO>()))
            .ReturnsAsync(true);
        var controller = new SuppliersController(mockRepo.Object);

        var result = await controller.UpdateSupplier(1, new SupplierUpdateDTO { Name = "Supplier A" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task DeleteSupplier_Should_Return_NotFound_When_Record_Does_Not_Exist()
    {
        var mockRepo = new Mock<ISupplierRepository>();
        mockRepo.Setup(repo => repo.DeleteSupplierAsync(1))
            .ReturnsAsync(SupplierDeleteStatus.NotFound);
        var controller = new SuppliersController(mockRepo.Object);

        var result = await controller.DeleteSupplier(1);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public async Task DeleteSupplier_Should_Return_Conflict_When_Linked_Records_Exist()
    {
        var mockRepo = new Mock<ISupplierRepository>();
        mockRepo.Setup(repo => repo.DeleteSupplierAsync(1))
            .ReturnsAsync(SupplierDeleteStatus.LinkedRecordsExist);
        var controller = new SuppliersController(mockRepo.Object);

        var result = await controller.DeleteSupplier(1);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task DeleteSupplier_Should_Return_Ok_When_Deleted()
    {
        var mockRepo = new Mock<ISupplierRepository>();
        mockRepo.Setup(repo => repo.DeleteSupplierAsync(1))
            .ReturnsAsync(SupplierDeleteStatus.Deleted);
        var controller = new SuppliersController(mockRepo.Object);

        var result = await controller.DeleteSupplier(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }
}
