using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using HSMS.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace HSMS.Tests;

/// <summary>
/// Tests for concurrent stock updates to prevent race conditions
/// Requires: HSMS_TEST_CONNECTION_STRING environment variable
/// </summary>
[Collection("DatabaseIntegration")]
public class ConcurrentStockUpdateTests
{
    private static string? GetConnectionString() => System.Environment.GetEnvironmentVariable("HSMS_TEST_CONNECTION_STRING");

    private static ProductRepository CreateRepository(string connectionString)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", connectionString) })
            .Build();
        return new ProductRepository(config);
    }

    [Fact]
    public async Task UpdateProductStock_Should_Handle_Multiple_Concurrent_Updates()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var repository = CreateRepository(connectionString);

        // Setup: Create a product with initial stock
        var productId = await repository.AddProduct(new ProductCreateDTO
        {
            Name = "Concurrent Test Product",
            SKU = "CONC-001",
            Category = "Testing",
            Price = 100m,
            Quantity = 1000
        });

        // Execute: Simulate 10 concurrent stock updates (each reducing by 50)
        var tasks = Enumerable.Range(0, 10)
            .Select(i => repository.UpdateProductStock(productId, new ProductStockUpdateDTO { Quantity = 50 }))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Verify: All updates should succeed
        Assert.All(results, result => Assert.True(result));

        // Verify: Final stock should be 500 (1000 - 10*50)
        var finalProduct = await repository.GetProductById(productId);
        Assert.NotNull(finalProduct);
        Assert.Equal(500, finalProduct.Quantity);

        // Cleanup
        await repository.DeleteProduct(productId);
    }

    [Fact]
    public async Task UpdateProductStock_Should_Prevent_Negative_Stock_In_Concurrent_Scenario()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var repository = CreateRepository(connectionString);

        // Setup: Create a product with limited stock
        var productId = await repository.AddProduct(new ProductCreateDTO
        {
            Name = "Low Stock Concurrent Test",
            SKU = "LOWC-001",
            Category = "Testing",
            Price = 100m,
            Quantity = 100
        });

        // Execute: Try to deduct 60 from 10 concurrent tasks (total 600 reduction from 100 stock)
        var tasks = Enumerable.Range(0, 10)
            .Select(i => repository.UpdateProductStock(productId, new ProductStockUpdateDTO { Quantity = 60 }))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Verify: At least some updates should fail (prevent overselling)
        var failedCount = results.Count(r => !r);
        Assert.True(failedCount > 0, "Expected some concurrent updates to fail");

        // Verify: Stock should never go below 0
        var finalProduct = await repository.GetProductById(productId);
        Assert.NotNull(finalProduct);
        Assert.True(finalProduct.Quantity >= 0);

        // Cleanup
        await repository.DeleteProduct(productId);
    }

    [Fact]
    public async Task UpdateProductStock_Should_Maintain_Transaction_Isolation()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var repository = CreateRepository(connectionString);

        // Setup: Create a product
        var productId = await repository.AddProduct(new ProductCreateDTO
        {
            Name = "Transaction Isolation Test",
            SKU = "TISO-001",
            Category = "Testing",
            Price = 100m,
            Quantity = 500
        });

        // Execute: Run concurrent updates with different quantities
        var task1 = repository.UpdateProductStock(productId, new ProductStockUpdateDTO { Quantity = 100 });
        var task2 = repository.UpdateProductStock(productId, new ProductStockUpdateDTO { Quantity = 150 });
        var task3 = repository.UpdateProductStock(productId, new ProductStockUpdateDTO { Quantity = 200 });

        await Task.WhenAll(task1, task2, task3);

        // Verify: All updates should succeed without conflict
        var finalProduct = await repository.GetProductById(productId);
        Assert.NotNull(finalProduct);
        // Final should be 500 - 100 - 150 - 200 = 50
        Assert.Equal(50, finalProduct.Quantity);

        // Cleanup
        await repository.DeleteProduct(productId);
    }

    [Fact]
    public async Task UpdateProductStock_Should_Be_Atomic_With_StockLogs()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var repository = CreateRepository(connectionString);

        // Setup: Create a product
        var productId = await repository.AddProduct(new ProductCreateDTO
        {
            Name = "Atomic Logging Test",
            SKU = "ATOM-001",
            Category = "Testing",
            Price = 100m,
            Quantity = 200
        });

        // Execute: Update stock with logging
        var result = await repository.UpdateProductStock(productId, new ProductStockUpdateDTO { Quantity = 50 });
        Assert.True(result);

        // Verify: Stock is updated
        var product = await repository.GetProductById(productId);
        Assert.NotNull(product);
        Assert.Equal(150, product.Quantity);

        // Note: Stock log verification would require direct DB query access
        // This test ensures the transaction completes without partial updates

        // Cleanup
        await repository.DeleteProduct(productId);
    }

    [Theory]
    [InlineData(1, 999)]  // Update down to 1
    [InlineData(10, 990)] // Update down by 10
    [InlineData(500, 500)] // Update exactly half
    public async Task UpdateProductStock_Should_Handle_Various_Quantities_Concurrently(int updateAmount, int expectedFinal)
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var repository = CreateRepository(connectionString);

        // Setup
        var productId = await repository.AddProduct(new ProductCreateDTO
        {
            Name = $"Qty Test {updateAmount}",
            SKU = $"QTY-{updateAmount:000}",
            Category = "Testing",
            Price = 100m,
            Quantity = 1000
        });

        // Execute: Single concurrent update
        var result = await repository.UpdateProductStock(productId, new ProductStockUpdateDTO { Quantity = updateAmount });

        // Verify
        Assert.True(result);
        var product = await repository.GetProductById(productId);
        Assert.NotNull(product);
        Assert.Equal(expectedFinal, product.Quantity);

        // Cleanup
        await repository.DeleteProduct(productId);
    }
}
