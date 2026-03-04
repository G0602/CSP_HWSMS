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
        EnsureProductsTableExists();
    }

    // CREATE
    /// <inheritdoc/>
    public async Task<int> AddProduct(ProductCreateDTO product)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = @"INSERT INTO Products (Name, SKU, Price, Quantity, Category)
                         VALUES (@Name, @SKU, @Price, @Quantity, @Category);
                         SELECT LAST_INSERT_ID();";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Name", product.Name);
        command.Parameters.AddWithValue("@SKU", product.SKU);
        command.Parameters.AddWithValue("@Price", product.Price);
        command.Parameters.AddWithValue("@Quantity", product.Quantity);
        command.Parameters.AddWithValue("@Category", product.Category);

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
                             Quantity=@Quantity, Category=@Category
                         WHERE Id=@Id";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Name", product.Name);
        command.Parameters.AddWithValue("@SKU", product.SKU);
        command.Parameters.AddWithValue("@Price", product.Price);
        command.Parameters.AddWithValue("@Quantity", product.Quantity);
        command.Parameters.AddWithValue("@Category", product.Category);

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

    /// <summary>
    /// Creates the <c>Products</c> table if it does not already exist.
    /// Executed synchronously during the repository constructor to guarantee
    /// the table is present before the first request is served.
    /// </summary>
    private void EnsureProductsTableExists()
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        const string tableSql = @"CREATE TABLE IF NOT EXISTS Products (
                                    Id INT AUTO_INCREMENT PRIMARY KEY,
                                    Name VARCHAR(255) NOT NULL,
                                    SKU VARCHAR(100) NOT NULL,
                                    Price DECIMAL(10,2) NOT NULL,
                                    Quantity INT NOT NULL,
                                    Category VARCHAR(255) NOT NULL,
                                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                                  );";

        using var command = new MySqlCommand(tableSql, connection);
        command.ExecuteNonQuery();
    }
}