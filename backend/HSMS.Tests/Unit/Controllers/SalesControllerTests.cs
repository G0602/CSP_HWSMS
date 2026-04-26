using System.Security.Claims;
using HSMS.API.Controllers;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HSMS.Tests;

public class SalesControllerTests
{
    private static SalesController CreateController(Mock<ISaleRepository> saleRepository, string? username = "cashier")
    {
        var controller = new SalesController(saleRepository.Object);
        if (username is not null)
        {
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.Name, username)
                    ], "TestAuth"))
                }
            };
        }

        return controller;
    }

    [Fact]
    public async Task CreateSale_Should_Return_BadRequest_When_Items_Are_Empty()
    {
        var saleRepository = new Mock<ISaleRepository>();
        var controller = CreateController(saleRepository);

        var result = await controller.CreateSale(new SaleCreateDTO { Items = [] });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Please add at least one item to the sale.", badRequest.Value);
        saleRepository.Verify(repo => repo.CreateSaleAsync(It.IsAny<SaleCreateDTO>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateSale_Should_Return_BadRequest_When_ProductId_Is_Invalid()
    {
        var controller = CreateController(new Mock<ISaleRepository>());

        var result = await controller.CreateSale(new SaleCreateDTO
        {
            Items = [new SaleItemCreateDTO { ProductId = 0, Quantity = 1 }]
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Each sale item must reference a valid product.", badRequest.Value);
    }

    [Fact]
    public async Task CreateSale_Should_Return_BadRequest_When_Quantity_Is_Invalid()
    {
        var controller = CreateController(new Mock<ISaleRepository>());

        var result = await controller.CreateSale(new SaleCreateDTO
        {
            Items = [new SaleItemCreateDTO { ProductId = 1, Quantity = 0 }]
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Each sale item quantity must be greater than zero.", badRequest.Value);
    }

    [Fact]
    public async Task CreateSale_Should_Return_BadRequest_When_Product_Is_Duplicated()
    {
        var controller = CreateController(new Mock<ISaleRepository>());

        var result = await controller.CreateSale(new SaleCreateDTO
        {
            Items =
            [
                new SaleItemCreateDTO { ProductId = 3, Quantity = 1 },
                new SaleItemCreateDTO { ProductId = 3, Quantity = 2 }
            ]
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Product 3 is already in the sale. Update its quantity instead of adding it twice.", badRequest.Value);
    }

    [Fact]
    public async Task CreateSale_Should_Pass_Authenticated_Username_To_Repository()
    {
        var saleRepository = new Mock<ISaleRepository>();
        var dto = new SaleCreateDTO
        {
            Items = [new SaleItemCreateDTO { ProductId = 2, Quantity = 3 }]
        };
        var response = new SaleResponseDTO
        {
            SaleId = 22,
            SoldBy = "cashier-one",
            SoldAt = DateTime.UtcNow,
            TotalAmount = 4500m
        };

        saleRepository.Setup(repo => repo.CreateSaleAsync(dto, "cashier-one")).ReturnsAsync(response);
        var controller = CreateController(saleRepository, "cashier-one");

        var result = await controller.CreateSale(dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<SaleResponseDTO>(ok.Value);
        Assert.Equal(22, payload.SaleId);
        Assert.Equal(4500m, payload.TotalAmount);
    }

    [Fact]
    public async Task CreateSale_Should_Return_BadRequest_When_Repository_Rejects_Transaction()
    {
        var saleRepository = new Mock<ISaleRepository>();
        saleRepository.Setup(repo => repo.CreateSaleAsync(It.IsAny<SaleCreateDTO>(), "cashier"))
            .ThrowsAsync(new InvalidOperationException("Insufficient stock for Hammer."));
        var controller = CreateController(saleRepository);

        var result = await controller.CreateSale(new SaleCreateDTO
        {
            Items = [new SaleItemCreateDTO { ProductId = 1, Quantity = 99 }]
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Insufficient stock for Hammer.", badRequest.Value);
    }

    [Fact]
    public async Task CreateSale_Should_Return_500_When_Repository_Throws_Unexpected_Error()
    {
        var saleRepository = new Mock<ISaleRepository>();
        saleRepository.Setup(repo => repo.CreateSaleAsync(It.IsAny<SaleCreateDTO>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("database unavailable"));
        var controller = CreateController(saleRepository);

        var result = await controller.CreateSale(new SaleCreateDTO
        {
            Items = [new SaleItemCreateDTO { ProductId = 1, Quantity = 1 }]
        });

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
        Assert.Equal("Failed to save sale transaction.", status.Value);
    }

    [Fact]
    public async Task GetHistory_Should_Return_BadRequest_When_Limit_Is_Invalid()
    {
        var saleRepository = new Mock<ISaleRepository>();
        var controller = CreateController(saleRepository);

        var result = await controller.GetHistory(null, null, null, 0);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Limit must be greater than zero.", badRequest.Value);
        saleRepository.Verify(repo => repo.GetSalesHistoryAsync(
            It.IsAny<int?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetHistory_Should_Return_Filtered_History()
    {
        var saleRepository = new Mock<ISaleRepository>();
        var fromDate = new DateTime(2026, 4, 1);
        var toDate = new DateTime(2026, 4, 30);
        var history = new List<SaleHistoryItemDTO>
        {
            new() { SaleId = 12, SoldAt = fromDate, TotalAmount = 1200m, SoldBy = "manager", ItemCount = 2 }
        };

        saleRepository.Setup(repo => repo.GetSalesHistoryAsync(12, fromDate, toDate, 25)).ReturnsAsync(history);
        var controller = CreateController(saleRepository);

        var result = await controller.GetHistory(12, fromDate, toDate, 25);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(history, ok.Value);
    }

    [Fact]
    public async Task GetDetails_Should_Return_NotFound_When_Sale_Does_Not_Exist()
    {
        var saleRepository = new Mock<ISaleRepository>();
        saleRepository.Setup(repo => repo.GetSaleDetailsAsync(404)).ReturnsAsync((SaleResponseDTO?)null);
        var controller = CreateController(saleRepository);

        var result = await controller.GetDetails(404);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Transaction not found.", notFound.Value);
    }

    [Fact]
    public async Task GetInvoice_Should_Return_Invoice_When_Sale_Exists()
    {
        var saleRepository = new Mock<ISaleRepository>();
        var invoice = new InvoiceResponseDTO
        {
            TransactionId = 9,
            InvoiceNumber = "INV-000009",
            Subtotal = 1000m,
            GrandTotal = 1000m
        };
        saleRepository.Setup(repo => repo.GetInvoiceAsync(9)).ReturnsAsync(invoice);
        var controller = CreateController(saleRepository);

        var result = await controller.GetInvoice(9);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(invoice, ok.Value);
    }
}
