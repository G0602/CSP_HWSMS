using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
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
/// Sprint 3 - EPIC 3.2: Supplier Management Integration Tests
/// Tests database persistence, referential integrity, and delete protection
/// Requires: HSMS_TEST_CONNECTION_STRING environment variable
/// </summary>
public class SupplierIntegrationTests
{
    private static string? GetConnectionString()
    {
        return System.Environment.GetEnvironmentVariable("HSMS_TEST_CONNECTION_STRING");
    }

    private static SupplierRepository CreateRepository(string connectionString)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", connectionString) })
            .Build();

        return new SupplierRepository(config);
    }

    #region Story S3-US-04: Add Supplier

    [Fact]
    public async Task AddSupplierAsync_Should_Persist_To_Database()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        var dto = new SupplierCreateDTO
        {
            Name = $"Integration Supplier {System.Guid.NewGuid()}",
            ContactInfo = "+94-11-1234567"
        };

        // Act
        int supplierId = await repository.AddSupplierAsync(dto);

        try
        {
            // Assert
            Assert.True(supplierId > 0);

            // Verify in database
            var suppliers = await repository.GetSuppliersAsync();
            Assert.Contains(suppliers, s => s.Id == supplierId && s.Name == dto.Name);
        }
        finally
        {
            await CleanupSupplierAsync(connectionString, supplierId);
        }
    }

    [Fact]
    public async Task AddSupplierAsync_Should_Generate_CreatedAt_Timestamp()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        var dto = new SupplierCreateDTO { Name = $"Time Test {System.Guid.NewGuid()}" };

        // Act
        int supplierId = await repository.AddSupplierAsync(dto);

        try
        {
            // Assert
            var suppliers = await repository.GetSuppliersAsync();
            var supplier = suppliers.FirstOrDefault(s => s.Id == supplierId);

            Assert.NotNull(supplier);
            Assert.NotEqual(default(System.DateTime), supplier.CreatedAt);
            Assert.True(supplier.CreatedAt <= System.DateTime.UtcNow);
        }
        finally
        {
            await CleanupSupplierAsync(connectionString, supplierId);
        }
    }

    #endregion

    #region Story S3-US-05: Update/Delete Supplier

    [Fact]
    public async Task UpdateSupplierAsync_Should_Persist_Changes_To_Database()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        int supplierId = await InsertTestSupplierAsync(connectionString, "Original Name", "original@contact");

        try
        {
            var updateDto = new SupplierUpdateDTO
            {
                Name = "Updated Name",
                ContactInfo = "updated@contact.com"
            };

            // Act
            bool updated = await repository.UpdateSupplierAsync(supplierId, updateDto);

            // Assert
            Assert.True(updated);

            var suppliers = await repository.GetSuppliersAsync();
            var supplier = suppliers.FirstOrDefault(s => s.Id == supplierId);

            Assert.NotNull(supplier);
            Assert.Equal("Updated Name", supplier.Name);
            Assert.Equal("updated@contact.com", supplier.ContactInfo);
        }
        finally
        {
            await CleanupSupplierAsync(connectionString, supplierId);
        }
    }

    [Fact]
    public async Task DeleteSupplierAsync_Should_Prevent_Delete_If_Linked_To_Products()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        int supplierId = await InsertTestSupplierAsync(connectionString, "Linked Supplier");
        string sku = $"LINKED-{System.Guid.NewGuid():N}";
        int productId = await InsertTestProductAsync(connectionString, "Linked Product", sku, 1500m, 10, "Tools", supplierId);

        try
        {
            // Act
            var result = await repository.DeleteSupplierAsync(supplierId);

            // Assert
            Assert.Equal(HSMS.Application.Interfaces.SupplierDeleteStatus.LinkedRecordsExist, result);

            // Verify supplier still exists
            var suppliers = await repository.GetSuppliersAsync();
            Assert.Contains(suppliers, s => s.Id == supplierId);
        }
        finally
        {
            await CleanupProductAsync(connectionString, productId);
            await CleanupSupplierAsync(connectionString, supplierId);
        }
    }

    [Fact]
    public async Task DeleteSupplierAsync_Should_Delete_When_Not_Linked()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        int supplierId = await InsertTestSupplierAsync(connectionString, "Unlinked Supplier");

        // Act
        var result = await repository.DeleteSupplierAsync(supplierId);

        // Assert
        Assert.Equal(HSMS.Application.Interfaces.SupplierDeleteStatus.Deleted, result);

        // Verify supplier deleted
        var suppliers = await repository.GetSuppliersAsync();
        Assert.DoesNotContain(suppliers, s => s.Id == supplierId);
    }

    #endregion

    #region Story S3-US-06: Link Supplier to Product

    [Fact]
    public async Task Product_Should_Maintain_Referential_Integrity_With_Supplier()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var productRepository = new ProductRepository(new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", connectionString) })
            .Build());

        int supplierId = await InsertTestSupplierAsync(connectionString, "Referential Test Supplier");
        string sku = $"REF-{System.Guid.NewGuid():N}";

        try
        {
            // Act - Create product with valid supplier
            var createDto = new ProductCreateDTO
            {
                Name = "Product with Supplier",
                SKU = sku,
                Price = 2000,
                Quantity = 15,
                Category = "Tools",
                SupplierId = supplierId
            };

            int productId = await productRepository.AddProduct(createDto);

            try
            {
                // Assert - Verify relationship
                var product = await productRepository.GetProductById(productId);
                Assert.NotNull(product);
                Assert.Equal(supplierId, product.SupplierId);
            }
            finally
            {
                await CleanupProductAsync(connectionString, productId);
            }
        }
        finally
        {
            await CleanupSupplierAsync(connectionString, supplierId);
        }
    }

    #endregion

    #region Helper Methods

    private static async Task<int> InsertTestSupplierAsync(
        string connectionString,
        string name,
        string? contactInfo = null)
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        const string query = @"INSERT INTO Suppliers (Name, ContactInfo)
                              VALUES (@Name, @ContactInfo);
                              SELECT LAST_INSERT_ID();";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Name", name);
        command.Parameters.AddWithValue("@ContactInfo", (object?)contactInfo ?? DBNull.Value);

        var result = await command.ExecuteScalarAsync();
        return System.Convert.ToInt32(result);
    }

    private static async Task CleanupSupplierAsync(string connectionString, int supplierId)
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        const string removeReferences = "UPDATE Products SET SupplierId = NULL WHERE SupplierId = @SupplierId";
        using var cmd1 = new MySqlCommand(removeReferences, connection);
        cmd1.Parameters.AddWithValue("@SupplierId", supplierId);
        await cmd1.ExecuteNonQueryAsync();

        const string deleteSupplier = "DELETE FROM Suppliers WHERE Id = @Id";
        using var cmd2 = new MySqlCommand(deleteSupplier, connection);
        cmd2.Parameters.AddWithValue("@Id", supplierId);
        await cmd2.ExecuteNonQueryAsync();
    }

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

    #endregion
}
