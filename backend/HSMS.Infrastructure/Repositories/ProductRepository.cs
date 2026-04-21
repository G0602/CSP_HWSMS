using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using HSMS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace HSMS.Infrastructure.Repositories;

/// <summary>
/// MySQL implementation of <see cref="IProductRepository"/>.
/// Uses raw ADO.NET (no ORM) to execute parameterised SQL queries against
/// the <c>Products</c> table in the <c>CSP_HSMS</c> database.
/// The table is created automatically on first startup if it does not exist.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly string _connectionString;

    /// <summary>
    /// Resolves the connection string from application configuration and ensures the
    /// <c>Products</c> table exists before any operation is attempted.
    /// </summary>
    /// <param name="configuration">The ASP.NET Core configuration abstraction (injected).</param>
    public ProductRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    // CREATE
    /// <inheritdoc/>
    public async Task<int> AddProduct(ProductCreateDTO product)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = @"INSERT INTO Products (Name, SKU, Price, Quantity, Category, SupplierId)
                         VALUES (@Name, @SKU, @Price, @Quantity, @Category, @SupplierId);
                         SELECT LAST_INSERT_ID();";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Name", product.Name);
        command.Parameters.AddWithValue("@SKU", product.SKU);
        command.Parameters.AddWithValue("@Price", product.Price);
        command.Parameters.AddWithValue("@Quantity", product.Quantity);
        command.Parameters.AddWithValue("@Category", product.Category);
        command.Parameters.AddWithValue("@SupplierId", (object?)product.SupplierId ?? DBNull.Value);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    // READ ALL
    /// <inheritdoc/>
    public async Task<List<Product>> GetAllProducts()
    {
        var products = new List<Product>();

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new MySqlCommand("SELECT * FROM Products", connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            products.Add(new Product
            {
                Id = Convert.ToInt32(reader["Id"]),
                Name = reader["Name"].ToString()!,
                SKU = reader["SKU"].ToString()!,
                Price = Convert.ToDecimal(reader["Price"]),
                Quantity = Convert.ToInt32(reader["Quantity"]),
                Category = reader["Category"].ToString()!,
                SupplierId = reader["SupplierId"] == DBNull.Value ? null : Convert.ToInt32(reader["SupplierId"]),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
            });
        }

        return products;
    }

    public async Task<List<Product>> GetLowStockProducts(int threshold)
    {
        var products = new List<Product>();

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string query = @"SELECT Id, Name, SKU, Price, Quantity, Category, SupplierId, CreatedAt
                               FROM Products
                               WHERE Quantity < @Threshold
                               ORDER BY Quantity ASC, Name ASC";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Threshold", Math.Max(1, threshold));

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            products.Add(new Product
            {
                Id = Convert.ToInt32(reader["Id"]),
                Name = reader["Name"].ToString()!,
                SKU = reader["SKU"].ToString()!,
                Price = Convert.ToDecimal(reader["Price"]),
                Quantity = Convert.ToInt32(reader["Quantity"]),
                Category = reader["Category"].ToString()!,
                SupplierId = reader["SupplierId"] == DBNull.Value ? null : Convert.ToInt32(reader["SupplierId"]),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
            });
        }

        return products;
    }

    // SEARCH
    /// <inheritdoc/>
    public async Task<List<Product>> SearchProducts(string query, int limit = 20)
    {
        var products = new List<Product>();

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT *
                             FROM Products
                             WHERE Name LIKE @Term OR SKU LIKE @Term OR Category LIKE @Term
                             ORDER BY Name ASC
                             LIMIT @Limit";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Term", $"%{query}%");
        command.Parameters.AddWithValue("@Limit", Math.Clamp(limit, 1, 100));

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            products.Add(new Product
            {
                Id = Convert.ToInt32(reader["Id"]),
                Name = reader["Name"].ToString()!,
                SKU = reader["SKU"].ToString()!,
                Price = Convert.ToDecimal(reader["Price"]),
                Quantity = Convert.ToInt32(reader["Quantity"]),
                Category = reader["Category"].ToString()!,
                SupplierId = reader["SupplierId"] == DBNull.Value ? null : Convert.ToInt32(reader["SupplierId"]),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
            });
        }

        return products;
    }

    // READ BY ID
    /// <inheritdoc/>
    public async Task<Product?> GetProductById(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new MySqlCommand("SELECT * FROM Products WHERE Id = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new Product
            {
                Id = Convert.ToInt32(reader["Id"]),
                Name = reader["Name"].ToString()!,
                SKU = reader["SKU"].ToString()!,
                Price = Convert.ToDecimal(reader["Price"]),
                Quantity = Convert.ToInt32(reader["Quantity"]),
                Category = reader["Category"].ToString()!,
                SupplierId = reader["SupplierId"] == DBNull.Value ? null : Convert.ToInt32(reader["SupplierId"]),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
            };
        }

        return null;
    }

    // UPDATE
    /// <inheritdoc/>
    public async Task<bool> UpdateProduct(int id, ProductUpdateDTO product)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = @"UPDATE Products
                         SET Name=@Name, SKU=@SKU, Price=@Price,
                             Quantity=@Quantity, Category=@Category, SupplierId=@SupplierId
                         WHERE Id=@Id";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Name", product.Name);
        command.Parameters.AddWithValue("@SKU", product.SKU);
        command.Parameters.AddWithValue("@Price", product.Price);
        command.Parameters.AddWithValue("@Quantity", product.Quantity);
        command.Parameters.AddWithValue("@Category", product.Category);
        command.Parameters.AddWithValue("@SupplierId", (object?)product.SupplierId ?? DBNull.Value);

        int rows = await command.ExecuteNonQueryAsync();
        return rows > 0;
    }

    // DELETE
    /// <inheritdoc/>
    public async Task<bool> DeleteProduct(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new MySqlCommand("DELETE FROM Products WHERE Id=@Id", connection);
        command.Parameters.AddWithValue("@Id", id);

        int rows = await command.ExecuteNonQueryAsync();
        return rows > 0;
    }

    // STOCK UPDATE
    /// <inheritdoc/>
    public async Task<bool> UpdateProductStock(int id, ProductStockUpdateDTO stockUpdate)
    {
        // Validate that quantity is non-negative
        if (stockUpdate.Quantity < 0)
        {
            return false;
        }

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            const string selectSql = "SELECT Quantity FROM Products WHERE Id = @Id FOR UPDATE";
            await using var selectCommand = new MySqlCommand(selectSql, connection, (MySqlTransaction)transaction);
            selectCommand.Parameters.AddWithValue("@Id", id);

            var currentQuantityResult = await selectCommand.ExecuteScalarAsync();
            if (currentQuantityResult is null)
            {
                await transaction.RollbackAsync();
                return false;
            }

            int previousQuantity = Convert.ToInt32(currentQuantityResult);

            const string updateSql = @"UPDATE Products
                                       SET Quantity = @Quantity
                                       WHERE Id = @Id";
            await using var updateCommand = new MySqlCommand(updateSql, connection, (MySqlTransaction)transaction);
            updateCommand.Parameters.AddWithValue("@Id", id);
            updateCommand.Parameters.AddWithValue("@Quantity", stockUpdate.Quantity);
            int updatedRows = await updateCommand.ExecuteNonQueryAsync();

            if (updatedRows == 0)
            {
                await transaction.RollbackAsync();
                return false;
            }

            const string logSql = @"INSERT INTO StockLogs (ProductId, PreviousQuantity, NewQuantity, ChangeAmount, Reason)
                                    VALUES (@ProductId, @PreviousQuantity, @NewQuantity, @ChangeAmount, @Reason)";
            await using var logCommand = new MySqlCommand(logSql, connection, (MySqlTransaction)transaction);
            logCommand.Parameters.AddWithValue("@ProductId", id);
            logCommand.Parameters.AddWithValue("@PreviousQuantity", previousQuantity);
            logCommand.Parameters.AddWithValue("@NewQuantity", stockUpdate.Quantity);
            logCommand.Parameters.AddWithValue("@ChangeAmount", stockUpdate.Quantity - previousQuantity);
            logCommand.Parameters.AddWithValue("@Reason", (object?)stockUpdate.Reason ?? DBNull.Value);
            await logCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
