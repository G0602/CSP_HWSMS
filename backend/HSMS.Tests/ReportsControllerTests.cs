using HSMS.API.Controllers;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HSMS.Tests;

public class ReportsControllerTests
{
    [Fact]
    public async Task GetDailySalesReport_Should_Return_Ok_With_Report_Data()
    {
        var saleRepo = new Mock<ISaleRepository>();
        saleRepo.Setup(repo => repo.GetDailySalesReportAsync())
            .ReturnsAsync(
            [
                new DailySalesReportItemDTO
                {
                    Date = new DateTime(2026, 4, 3),
                    TotalAmount = 15000m
                },
                new DailySalesReportItemDTO
                {
                    Date = new DateTime(2026, 4, 2),
                    TotalAmount = 9800m
                }
            ]);

        var controller = new ReportsController(saleRepo.Object);
        var result = await controller.GetDailySalesReport();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task GetMonthlySalesReport_Should_Return_Ok_With_Report_Data()
    {
        var saleRepo = new Mock<ISaleRepository>();
        saleRepo.Setup(repo => repo.GetMonthlySalesReportAsync())
            .ReturnsAsync(
            [
                new MonthlySalesReportItemDTO
                {
                    Month = new DateTime(2026, 4, 1),
                    TotalAmount = 320000m
                },
                new MonthlySalesReportItemDTO
                {
                    Month = new DateTime(2026, 3, 1),
                    TotalAmount = 287500m
                }
            ]);

        var controller = new ReportsController(saleRepo.Object);
        var result = await controller.GetMonthlySalesReport();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }
}
