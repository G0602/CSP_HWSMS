using Xunit;
using Moq;
using HSMS.API.Controllers;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using HSMS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HSMS.Tests;

/// <summary>
/// Sprint 3 - EPIC 3.2: Supplier Management Unit Tests
/// Tests for stories: S3-US-04, S3-US-05, S3-US-06
/// </summary>
public class SupplierManagementTests
{
    #region Story S3-US-04: Add Supplier

    [Fact]
    public async Task AddSupplier_Should_Create_With_Name_And_Contact_Info()
    {
        // Arrange
        var mockRepo = new Mock<ISupplierRepository>();
        mockRepo.Setup(r => r.AddSupplierAsync(It.IsAny<SupplierCreateDTO>())).ReturnsAsync(1);

        var controller = new SuppliersController(mockRepo.Object);
        var dto = new SupplierCreateDTO
        {
            Name = "ABC Hardware Supplies",
            ContactInfo = "+94-11-2345678"
        };

        // Act
        var result = await controller.AddSupplier(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        mockRepo.Verify(r => r.AddSupplierAsync(It.Is<SupplierCreateDTO>(
            x => x.Name == "ABC Hardware Supplies")), Times.Once);
    }

    [Fact]
    public async Task AddSupplier_Should_Return_Created_With_Supplier_ID()
    {
        // Arrange
        var mockRepo = new Mock<ISupplierRepository>();
        mockRepo.Setup(r => r.AddSupplierAsync(It.IsAny<SupplierCreateDTO>())).ReturnsAsync(42);

        var controller = new SuppliersController(mockRepo.Object);
        var dto = new SupplierCreateDTO { Name = "New Supplier" };

        // Act
        var result = await controller.AddSupplier(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
    }

    [Fact]
    public async Task AddSupplier_Should_Return_BadRequest_When_Name_Is_Empty()
    {
        // Arrange
        var mockRepo = new Mock<ISupplierRepository>();
        var controller = new SuppliersController(mockRepo.Object);
        var dto = new SupplierCreateDTO { Name = "   " };

        // Act
        var result = await controller.AddSupplier(dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
        mockRepo.Verify(r => r.AddSupplierAsync(It.IsAny<SupplierCreateDTO>()), Times.Never);
    }

    [Fact]
    public async Task AddSupplier_Should_Trim_Whitespace_From_Name()
    {
        // Arrange
        var mockRepo = new Mock<ISupplierRepository>();
        mockRepo.Setup(r => r.AddSupplierAsync(It.IsAny<SupplierCreateDTO>())).ReturnsAsync(1);

        var controller = new SuppliersController(mockRepo.Object);
        var dto = new SupplierCreateDTO { Name = "   Supplier Name   " };

        // Act
        var result = await controller.AddSupplier(dto);

        // Assert
        mockRepo.Verify(r => r.AddSupplierAsync(It.Is<SupplierCreateDTO>(
            x => x.Name == "Supplier Name")), Times.Once);
    }

    #endregion

    #region Story S3-US-05: Update/Delete Supplier

    [Fact]
    public async Task UpdateSupplier_Should_Modify_Name_And_Contact_Info()
    {
        // Arrange
        var mockRepo = new Mock<ISupplierRepository>();
        mockRepo.Setup(r => r.UpdateSupplierAsync(1, It.IsAny<SupplierUpdateDTO>())).ReturnsAsync(true);

        var controller = new SuppliersController(mockRepo.Object);
        var dto = new SupplierUpdateDTO
        {
            Name = "Updated Name",
            ContactInfo = "new@supplier.com"
        };

        // Act
        var result = await controller.UpdateSupplier(1, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        mockRepo.Verify(r => r.UpdateSupplierAsync(1, It.Is<SupplierUpdateDTO>(
            x => x.Name == "Updated Name")), Times.Once);
    }

    [Fact]
    public async Task UpdateSupplier_Should_Return_NotFound_When_Supplier_Not_Exists()
    {
        // Arrange
        var mockRepo = new Mock<ISupplierRepository>();
        mockRepo.Setup(r => r.UpdateSupplierAsync(999, It.IsAny<SupplierUpdateDTO>())).ReturnsAsync(false);

        var controller = new SuppliersController(mockRepo.Object);
        var dto = new SupplierUpdateDTO { Name = "New Name" };

        // Act
        var result = await controller.UpdateSupplier(999, dto);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public async Task UpdateSupplier_Should_Return_BadRequest_When_Name_Is_Empty()
    {
        // Arrange
        var mockRepo = new Mock<ISupplierRepository>();
        var controller = new SuppliersController(mockRepo.Object);
        var dto = new SupplierUpdateDTO { Name = "  " };

        // Act
        var result = await controller.UpdateSupplier(1, dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task DeleteSupplier_Should_Remove_When_Not_Linked_To_Products()
    {
        // Arrange
        var mockRepo = new Mock<ISupplierRepository>();
        mockRepo.Setup(r => r.DeleteSupplierAsync(1)).ReturnsAsync(SupplierDeleteStatus.Deleted);

        var controller = new SuppliersController(mockRepo.Object);

        // Act
        var result = await controller.DeleteSupplier(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        mockRepo.Verify(r => r.DeleteSupplierAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteSupplier_Should_Return_Conflict_When_Linked_To_Products()
    {
        // Arrange
        var mockRepo = new Mock<ISupplierRepository>();
        mockRepo.Setup(r => r.DeleteSupplierAsync(5)).ReturnsAsync(SupplierDeleteStatus.LinkedRecordsExist);

        var controller = new SuppliersController(mockRepo.Object);

        // Act
        var result = await controller.DeleteSupplier(5);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflictResult.StatusCode);
        Assert.Contains("linked", conflictResult.Value?.ToString() ?? "", System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteSupplier_Should_Return_NotFound_When_Supplier_Not_Found()
    {
        // Arrange
        var mockRepo = new Mock<ISupplierRepository>();
        mockRepo.Setup(r => r.DeleteSupplierAsync(999)).ReturnsAsync(SupplierDeleteStatus.NotFound);

        var controller = new SuppliersController(mockRepo.Object);

        // Act
        var result = await controller.DeleteSupplier(999);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public async Task DeleteSupplier_Should_Prevent_Delete_If_Linked_To_Products_BONUS()
    {
        // Arrange - This tests the BONUS feature: "Prevent delete if linked to products"
        var mockRepo = new Mock<ISupplierRepository>();
        mockRepo.Setup(r => r.DeleteSupplierAsync(10))
            .ReturnsAsync(SupplierDeleteStatus.LinkedRecordsExist);

        var controller = new SuppliersController(mockRepo.Object);

        // Act
        var result = await controller.DeleteSupplier(10);

        // Assert - Should return 409 Conflict (not deleted)
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflictResult.StatusCode);
    }

    #endregion

    #region Story S3-US-06: Link Supplier to Product

    [Fact]
    public async Task AddProduct_Should_Accept_Valid_SupplierId()
    {
        // Arrange
        var mockProductRepo = new Mock<IProductRepository>();
        var mockSupplierRepo = new Mock<ISupplierRepository>();

        mockProductRepo.Setup(r => r.AddProduct(It.IsAny<ProductCreateDTO>())).ReturnsAsync(1);
        mockSupplierRepo.Setup(r => r.SupplierExistsAsync(5)).ReturnsAsync(true);

        var controller = new ProductController(mockProductRepo.Object, null, mockSupplierRepo.Object);
        var dto = new ProductCreateDTO
        {
            Name = "Product",
            SKU = "P-001",
            Price = 1500,
            Quantity = 10,
            Category = "Tools",
            SupplierId = 5
        };

        // Act
        var result = await controller.AddProduct(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        mockProductRepo.Verify(r => r.AddProduct(It.Is<ProductCreateDTO>(
            x => x.SupplierId == 5)), Times.Once);
    }

    [Fact]
    public async Task AddProduct_Should_Reject_Invalid_SupplierId()
    {
        // Arrange
        var mockProductRepo = new Mock<IProductRepository>();
        var mockSupplierRepo = new Mock<ISupplierRepository>();

        mockSupplierRepo.Setup(r => r.SupplierExistsAsync(999)).ReturnsAsync(false);

        var controller = new ProductController(mockProductRepo.Object, null, mockSupplierRepo.Object);
        var dto = new ProductCreateDTO
        {
            Name = "Product",
            SKU = "P-001",
            Price = 1000,
            Quantity = 10,
            Category = "Tools",
            SupplierId = 999  // Non-existent
        };

        // Act
        var result = await controller.AddProduct(dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_Should_Change_Supplier_Link()
    {
        // Arrange
        var mockProductRepo = new Mock<IProductRepository>();
        var mockSupplierRepo = new Mock<ISupplierRepository>();

        mockProductRepo.Setup(r => r.UpdateProduct(1, It.IsAny<ProductUpdateDTO>())).ReturnsAsync(true);
        mockSupplierRepo.Setup(r => r.SupplierExistsAsync(10)).ReturnsAsync(true);

        var controller = new ProductController(mockProductRepo.Object, null, mockSupplierRepo.Object);
        var dto = new ProductUpdateDTO
        {
            Name = "Updated",
            SKU = "U-001",
            Price = 2000,
            Quantity = 20,
            Category = "Tools",
            SupplierId = 10
        };

        // Act
        var result = await controller.UpdateProduct(1, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        mockProductRepo.Verify(r => r.UpdateProduct(1, It.Is<ProductUpdateDTO>(
            x => x.SupplierId == 10)), Times.Once);
    }

    [Fact]
    public async Task UpdateProduct_Should_Allow_Removing_Supplier()
    {
        // Arrange
        var mockProductRepo = new Mock<IProductRepository>();
        var mockSupplierRepo = new Mock<ISupplierRepository>();

        mockProductRepo.Setup(r => r.UpdateProduct(1, It.IsAny<ProductUpdateDTO>())).ReturnsAsync(true);
        var controller = new ProductController(mockProductRepo.Object, null, mockSupplierRepo.Object);
        var dto = new ProductUpdateDTO
        {
            Name = "Product",
            SKU = "P-001",
            Price = 1500,
            Quantity = 10,
            Category = "Tools",
            SupplierId = null
        };

        // Act
        var result = await controller.UpdateProduct(1, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    #endregion
}
