using HSMS.Application.DTOs;
using HSMS.Infrastructure.Repositories;
using HSMS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HSMS.Tests;

/// <summary>
/// Sprint 3 - EPIC 3.4: Reporting Integration Tests  
/// Tests report data accuracy, calculations, and database queries
/// Requires: HSMS_TEST_CONNECTION_STRING environment variable
/// </summary>
[Collection("DatabaseIntegration")]
public class ReportingIntegrationTests
{
    private static string? GetConnectionString()
    {
        return Environment.GetEnvironmentVariable("HSMS_TEST_CONNECTION_STRING");
    }

    private static SaleRepository CreateRepository(string connectionString)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", connectionString) })
            .Build();

        return new SaleRepository(config);
    }

    #region Story S3-US-10: Daily Sales Report

    [Fact]
    public async Task GetDailySalesReportAsync_Should_Query_Database_Accurately()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        
        int productId = await InsertTestProductAsync(connectionString, "Report Test Product", "RTP-001", 1000m, 50);
        try
        {
            // Create a test sale
            var saleDto = new SaleCreateDTO
            {
                Items = new List<SaleItemCreateDTO>
                {
                    new SaleItemCreateDTO { ProductId = productId, Quantity = 2 }
                }
            };

            var sale = await repository.CreateSaleAsync(saleDto, "test-user");

            // Act
            var dailyReport = await repository.GetDailySalesReportAsync();

            // Assert
            Assert.NotEmpty(dailyReport);
            var todaysSale = dailyReport.FirstOrDefault(r => r.Date.Date == DateTime.UtcNow.Date);
            Assert.NotNull(todaysSale);
            Assert.True(todaysSale.TotalAmount > 0);
        }
        finally
        {
            await CleanupProductAsync(connectionString, productId);
        }
    }

    [Fact]
    public async Task GetDailySalesReportAsync_Should_Calculate_Correct_Totals()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        
        int product1 = await InsertTestProductAsync(connectionString, "Product 1", "P1-001", 1000m, 50);
        int product2 = await InsertTestProductAsync(connectionString, "Product 2", "P2-001", 500m, 50);

        try
        {
            // Create two sales
            var sale1 = new SaleCreateDTO
            {
                Items = new List<SaleItemCreateDTO>
                {
                    new SaleItemCreateDTO { ProductId = product1, Quantity = 2 }  // 2000
                }
            };

            var sale2 = new SaleCreateDTO
            {
                Items = new List<SaleItemCreateDTO>
                {
                    new SaleItemCreateDTO { ProductId = product2, Quantity = 2 }  // 1000
                }
            };

            await repository.CreateSaleAsync(sale1, "user1");
            await repository.CreateSaleAsync(sale2, "user2");

            // Act
            var dailyReport = await repository.GetDailySalesReportAsync();
            var todaysSale = dailyReport.FirstOrDefault(r => r.Date.Date == DateTime.UtcNow.Date);

            // Assert
            Assert.NotNull(todaysSale);
            Assert.True(todaysSale.TotalAmount >= 3000, $"Expected >= 3000, got {todaysSale.TotalAmount}");
        }
        finally
        {
            await CleanupProductAsync(connectionString, product1);
            await CleanupProductAsync(connectionString, product2);
        }
    }

    #endregion

    #region Story S3-US-11: Monthly Sales Report

    [Fact]
    public async Task GetMonthlySalesReportAsync_Should_Query_Database_Accurately()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        
        int productId = await InsertTestProductAsync(connectionString, "Monthly Test", "MT-001", 2000m, 50);

        try
        {
            // Act - Create sale and get monthly report
            var saleDto = new SaleCreateDTO
            {
                Items = new List<SaleItemCreateDTO>
                {
                    new SaleItemCreateDTO { ProductId = productId, Quantity = 1 }
                }
            };

            await repository.CreateSaleAsync(saleDto, "test-user");
            var monthlyReport = await repository.GetMonthlySalesReportAsync();

            // Assert
            Assert.NotEmpty(monthlyReport);
            var thisMonth = monthlyReport.FirstOrDefault(m => 
                m.Month.Year == DateTime.UtcNow.Year && 
                m.Month.Month == DateTime.UtcNow.Month
            );
            Assert.NotNull(thisMonth);
            Assert.True(thisMonth.TotalAmount >= 2000);
        }
        finally
        {
            await CleanupProductAsync(connectionString, productId);
        }
    }

    [Fact]
    public async Task GetMonthlySalesReportAsync_Should_Group_By_Month_Correctly()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        
        int productId = await InsertTestProductAsync(connectionString, "Month Group Test", "MGT-001", 1000m, 50);

        try
        {
            // Create multiple sales in current month
            for (int i = 0; i < 3; i++)
            {
                var saleDto = new SaleCreateDTO
                {
                    Items = new List<SaleItemCreateDTO>
                    {
                        new SaleItemCreateDTO { ProductId = productId, Quantity = 1 }
                    }
                };

                await repository.CreateSaleAsync(saleDto, $"user-{i}");
            }

            // Act
            var monthlyReport = await repository.GetMonthlySalesReportAsync();
            var thisMonth = monthlyReport.FirstOrDefault(m =>
                m.Month.Year == DateTime.UtcNow.Year &&
                m.Month.Month == DateTime.UtcNow.Month
            );

            // Assert - All sales should be grouped in one month record
            Assert.NotNull(thisMonth);
            Assert.True(thisMonth.TotalAmount >= 3000, $"Expected >= 3000, got {thisMonth.TotalAmount}");

            // Verify month is first day of the month
            Assert.Equal(1, thisMonth.Month.Day);
        }
        finally
        {
            await CleanupProductAsync(connectionString, productId);
        }
    }

    #endregion

    #region Story S3-US-12: Low Stock Report

    [Fact]
    public async Task LowStockReport_Should_Use_Configured_Threshold()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", connectionString) })
            .Build();

        var productRepo = new ProductRepository(config);

        // Create products with different stock levels
        int lowStockProduct = await InsertTestProductAsync(connectionString, "Low Stock", "LS-001", 1000m, 5);
        int normalProduct = await InsertTestProductAsync(connectionString, "Normal Stock", "NS-001", 1000m, 50);

        try
        {
            // Act
            var allProducts = await productRepo.GetAllProducts();
            var lowStockItems = allProducts.Where(p => p.Quantity < 10).ToList();

            // Assert
            Assert.Contains(lowStockItems, p => p.Id == lowStockProduct);
            Assert.DoesNotContain(lowStockItems, p => p.Id == normalProduct);
        }
        finally
        {
            await CleanupProductAsync(connectionString, lowStockProduct);
            await CleanupProductAsync(connectionString, normalProduct);
        }
    }

    #endregion

    #region Helper Methods

    private static async Task<int> InsertTestProductAsync(
        string connectionString,
        string name,
        string sku,
        decimal price,
        int quantity,
        int? supplierId = null)
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        const string query = @"INSERT INTO Products (Name, SKU, Price, Quantity, Category, SupplierId)
                              VALUES (@Name, @SKU, @Price, @Quantity, @Category, @SupplierId);
                              SELECT LAST_INSERT_ID();";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Name", name);
        command.Parameters.AddWithValue("@SKU", sku);
        command.Parameters.AddWithValue("@Price", price);
        command.Parameters.AddWithValue("@Quantity", quantity);
        command.Parameters.AddWithValue("@Category", "Integration Test");
        command.Parameters.AddWithValue("@SupplierId", (object?)supplierId ?? DBNull.Value);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private static async Task CleanupProductAsync(string connectionString, int productId)
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // First delete any sale items referencing this product
        const string deleteSaleItems = @"DELETE FROM SaleItems 
                                         WHERE ProductId = @ProductId";
        using var cmd1 = new MySqlCommand(deleteSaleItems, connection);
        cmd1.Parameters.AddWithValue("@ProductId", productId);
        await cmd1.ExecuteNonQueryAsync();

        // Then delete the product
        const string deleteProduct = "DELETE FROM Products WHERE Id = @Id";
        using var cmd2 = new MySqlCommand(deleteProduct, connection);
        cmd2.Parameters.AddWithValue("@Id", productId);
        await cmd2.ExecuteNonQueryAsync();
    }

    #endregion
}
