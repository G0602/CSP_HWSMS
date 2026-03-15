using HSMS.Application.DTOs;
using HSMS.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace HSMS.Tests;

public class SaleRepositoryIntegrationTests
{
    private static string? GetConnectionString()
    {
        return Environment.GetEnvironmentVariable("HSMS_TEST_CONNECTION_STRING");
    }

    private static SaleRepository CreateRepository(string connectionString)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", connectionString),
            ])
            .Build();

        return new SaleRepository(config);
    }

    [Fact]
    public async Task CreateSaleAsync_Should_Deduct_Stock_When_Sale_Succeeds()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var repository = CreateRepository(connectionString);

        string sku = $"IT-SALE-{Guid.NewGuid():N}";
        int productId = await InsertProductAsync(connectionString, "Integration Hammer", sku, 1000m, 15, "Tools");

        try
        {
            var sale = new SaleCreateDTO
            {
                Items = [new SaleItemCreateDTO { ProductId = productId, Quantity = 4 }],
            };

            var result = await repository.CreateSaleAsync(sale, "integration-admin");

            Assert.True(result.SaleId > 0);
            Assert.Equal(4000m, result.TotalAmount);

            int remainingStock = await GetProductQuantityAsync(connectionString, productId);
            Assert.Equal(11, remainingStock);
        }
        finally
        {
            await DeleteProductCascadeAsync(connectionString, productId);
        }
    }

    [Fact]
    public async Task CreateSaleAsync_Should_Rollback_When_Stock_Is_Insufficient()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var repository = CreateRepository(connectionString);

        string sku = $"IT-ROLLBACK-{Guid.NewGuid():N}";
        int productId = await InsertProductAsync(connectionString, "Integration Drill", sku, 2500m, 2, "Power Tools");
        int salesCountBefore = await GetSalesCountForProductAsync(connectionString, productId);

        try
        {
            var sale = new SaleCreateDTO
            {
                Items = [new SaleItemCreateDTO { ProductId = productId, Quantity = 5 }],
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CreateSaleAsync(sale, "integration-manager"));

            int remainingStock = await GetProductQuantityAsync(connectionString, productId);
            Assert.Equal(2, remainingStock);

            int salesCountAfter = await GetSalesCountForProductAsync(connectionString, productId);
            Assert.Equal(salesCountBefore, salesCountAfter);
        }
        finally
        {
            await DeleteProductCascadeAsync(connectionString, productId);
        }
    }

    private static async Task<int> InsertProductAsync(
        string connectionString,
        string name,
        string sku,
        decimal price,
        int quantity,
        string category)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"INSERT INTO Products (Name, SKU, Price, Quantity, Category)
                             VALUES (@Name, @SKU, @Price, @Quantity, @Category);
                             SELECT LAST_INSERT_ID();";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Name", name);
        command.Parameters.AddWithValue("@SKU", sku);
        command.Parameters.AddWithValue("@Price", price);
        command.Parameters.AddWithValue("@Quantity", quantity);
        command.Parameters.AddWithValue("@Category", category);

        object? result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private static async Task<int> GetProductQuantityAsync(string connectionString, int productId)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = "SELECT Quantity FROM Products WHERE Id = @Id";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", productId);

        object? result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private static async Task<int> GetSalesCountForProductAsync(string connectionString, int productId)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = "SELECT COUNT(*) FROM SaleItems WHERE ProductId = @ProductId";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProductId", productId);

        object? result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private static async Task DeleteProductCascadeAsync(string connectionString, int productId)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        const string deleteSaleItemsSql = @"DELETE FROM SaleItems
                                            WHERE ProductId = @ProductId";

        await using (var deleteSaleItemsCommand = new MySqlCommand(deleteSaleItemsSql, connection))
        {
            deleteSaleItemsCommand.Parameters.AddWithValue("@ProductId", productId);
            await deleteSaleItemsCommand.ExecuteNonQueryAsync();
        }

        const string deleteSalesSql = @"DELETE s FROM Sales s
                                        LEFT JOIN SaleItems si ON si.SaleId = s.Id
                                        WHERE si.Id IS NULL";

        await using (var deleteSalesCommand = new MySqlCommand(deleteSalesSql, connection))
        {
            await deleteSalesCommand.ExecuteNonQueryAsync();
        }

        const string deleteProductSql = "DELETE FROM Products WHERE Id = @Id";
        await using var deleteProductCommand = new MySqlCommand(deleteProductSql, connection);
        deleteProductCommand.Parameters.AddWithValue("@Id", productId);
        await deleteProductCommand.ExecuteNonQueryAsync();
    }
}
