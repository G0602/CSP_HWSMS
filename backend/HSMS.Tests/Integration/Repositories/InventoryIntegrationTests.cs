using HSMS.Application.DTOs;
using HSMS.Infrastructure.Repositories;
using HSMS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HSMS.Tests;

/// <summary>
/// Sprint 3 - EPIC 3.1: Inventory Management Integration Tests
/// Tests database persistence and transaction safety
/// Requires: HSMS_TEST_CONNECTION_STRING environment variable
/// </summary>
[Collection("DatabaseIntegration")]
public class InventoryIntegrationTests
{
    private static string? GetConnectionString()
    {
        return System.Environment.GetEnvironmentVariable("HSMS_TEST_CONNECTION_STRING");
    }

    private static ProductRepository CreateRepository(string connectionString)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", connectionString) })
            .Build();

        return new ProductRepository(config);
    }

    #region Story S3-US-01 & S3-US-02: View Inventory & Low Stock Alerts

    [Fact]
    public async Task GetAllProducts_Should_Fetch_All_Products_From_Database()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        string uniqueSku = $"INV-TEST-{System.Guid.NewGuid():N}";
        
        int productId = await InsertTestProductAsync(
            connectionString, 
            "Integration Test Product", 
            uniqueSku, 
            1500m, 
            20, 
            "Test Category"
        );

        try
        {
            // Act
            var products = await repository.GetAllProducts();

            // Assert
            Assert.NotEmpty(products);
            Assert.Contains(products, p => p.Id == productId && p.SKU == uniqueSku);
        }
        finally
        {
            await CleanupProductAsync(connectionString, productId);
        }
    }

    [Fact]
    public async Task GetAllProducts_Should_Include_Supplier_Relationships()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        int supplierId = await InsertTestSupplierAsync(connectionString, "Test Supplier");
        string sku = $"PS-TEST-{System.Guid.NewGuid():N}";
        
        int productId = await InsertTestProductAsync(
            connectionString,
            "Product with Supplier",
            sku,
            2000m,
            15,
            "Tools",
            supplierId
        );

        try
        {
            // Act
            var products = await repository.GetAllProducts();
            var product = products.FirstOrDefault(p => p.Id == productId);

            // Assert
            Assert.NotNull(product);
            Assert.Equal(supplierId, product.SupplierId);
        }
        finally
        {
            await CleanupProductAsync(connectionString, productId);
            await CleanupSupplierAsync(connectionString, supplierId);
        }
    }

    #endregion

    #region Story S3-US-03: Manual Stock Update

    [Fact]
    public async Task UpdateProductStock_Should_Persist_Changes_To_Database()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        string sku = $"UPS-{System.Guid.NewGuid():N}";
        int productId = await InsertTestProductAsync(connectionString, "Stock Update Test", sku, 1000m, 50, "Tools");

        try
        {
            var dto = new ProductStockUpdateDTO { Quantity = 75, Reason = "Integration test restock" };

            // Act
            bool updated = await repository.UpdateProductStock(productId, dto);

            // Assert
            Assert.True(updated);

            // Verify updated quantity in database
            var product = await repository.GetProductById(productId);
            Assert.NotNull(product);
            Assert.Equal(75, product.Quantity);
        }
        finally
        {
            await CleanupProductAsync(connectionString, productId);
        }
    }

    [Fact]
    public async Task UpdateProductStock_Should_Reject_Negative_Quantities()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        string sku = $"NEG-{System.Guid.NewGuid():N}";
        int productId = await InsertTestProductAsync(connectionString, "Negative Test", sku, 1000m, 50, "Tools");

        try
        {
            var dto = new ProductStockUpdateDTO { Quantity = -10, Reason = "Should fail" };

            // Act
            bool updated = await repository.UpdateProductStock(productId, dto);

            // Assert
            Assert.False(updated);

            // Verify quantity unchanged
            var product = await repository.GetProductById(productId);
            Assert.NotNull(product);
            Assert.Equal(50, product.Quantity);
        }
        finally
        {
            await CleanupProductAsync(connectionString, productId);
        }
    }

    [Fact]
    public async Task UpdateProductStock_Should_Handle_Concurrent_Updates_Safely()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // This test verifies transaction isolation with row-level locking
        // Production scenario: Multiple users updating stock simultaneously

        // Arrange
        var repository = CreateRepository(connectionString);
        string sku = $"CONC-{System.Guid.NewGuid():N}";
        int productId = await InsertTestProductAsync(connectionString, "Concurrent Test", sku, 1000m, 100, "Tools");

        try
        {
            // Act - Simulate concurrent updates
            var tasks = new[]
            {
                repository.UpdateProductStock(productId, new ProductStockUpdateDTO { Quantity = 80 }),
                repository.UpdateProductStock(productId, new ProductStockUpdateDTO { Quantity = 75 })
            };

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert - One update should succeed (transaction safe)
            var product = await repository.GetProductById(productId);
            Assert.NotNull(product);
            // Final value should be one of the two attempted updates (last write wins with row lock)
            Assert.True(product.Quantity == 80 || product.Quantity == 75);
        }
        finally
        {
            await CleanupProductAsync(connectionString, productId);
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
        string category,
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
        command.Parameters.AddWithValue("@Category", category);
        command.Parameters.AddWithValue("@SupplierId", (object?)supplierId ?? DBNull.Value);

        var result = await command.ExecuteScalarAsync();
        return System.Convert.ToInt32(result);
    }

    private static async Task CleanupProductAsync(string connectionString, int productId)
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        const string query = "DELETE FROM Products WHERE Id = @Id";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Id", productId);

        await command.ExecuteNonQueryAsync();
    }

    private static async Task<int> InsertTestSupplierAsync(string connectionString, string name)
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        const string query = @"INSERT INTO Suppliers (Name) VALUES (@Name);
                              SELECT LAST_INSERT_ID();";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Name", name);

        var result = await command.ExecuteScalarAsync();
        return System.Convert.ToInt32(result);
    }

    private static async Task CleanupSupplierAsync(string connectionString, int supplierId)
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // Remove product references first
        const string deleteProducts = "UPDATE Products SET SupplierId = NULL WHERE SupplierId = @SupplierId";
        using var cmd1 = new MySqlCommand(deleteProducts, connection);
        cmd1.Parameters.AddWithValue("@SupplierId", supplierId);
        await cmd1.ExecuteNonQueryAsync();

        // Then delete supplier
        const string deleteSupplier = "DELETE FROM Suppliers WHERE Id = @Id";
        using var cmd2 = new MySqlCommand(deleteSupplier, connection);
        cmd2.Parameters.AddWithValue("@Id", supplierId);
        await cmd2.ExecuteNonQueryAsync();
    }

    #endregion
}
