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
    private static readonly object SchemaLock = new();
    private static bool _schemaInitialized;

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

    /// <summary>
    /// Creates the <c>Products</c> table if it does not already exist.
    /// Executed synchronously during the repository constructor to guarantee
    /// the table is present before the first request is served.
    /// </summary>
    private void EnsureProductsTableExists()
    {
        if (_schemaInitialized)
        {
            return;
        }

        lock (SchemaLock)
        {
            if (_schemaInitialized)
            {
                return;
            }

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        const string tableSql = @"CREATE TABLE IF NOT EXISTS Products (
                                    Id INT AUTO_INCREMENT PRIMARY KEY,
                                    Name VARCHAR(255) NOT NULL,
                                    SKU VARCHAR(100) NOT NULL,
                                    Price DECIMAL(10,2) NOT NULL,
                                    Quantity INT NOT NULL,
                                    Category VARCHAR(255) NOT NULL,
                                    SupplierId INT NULL,
                                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                                  );";

        using var command = new MySqlCommand(tableSql, connection);
        command.ExecuteNonQuery();

        EnsureColumnExists(connection, "Products", "Price", "ALTER TABLE Products ADD COLUMN Price DECIMAL(10,2) NOT NULL DEFAULT 0;");
        EnsureColumnExists(connection, "Products", "Quantity", "ALTER TABLE Products ADD COLUMN Quantity INT NOT NULL DEFAULT 0;");
        EnsureColumnExists(connection, "Products", "Category", "ALTER TABLE Products ADD COLUMN Category VARCHAR(255) NOT NULL DEFAULT '';");
        EnsureColumnExists(connection, "Products", "SupplierId", "ALTER TABLE Products ADD COLUMN SupplierId INT NULL;");

        const string suppliersTableSql = @"CREATE TABLE IF NOT EXISTS Suppliers (
                                            Id INT AUTO_INCREMENT PRIMARY KEY,
                                            Name VARCHAR(255) NOT NULL,
                                            ContactInfo VARCHAR(255) NULL,
                                            CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                                          );";
        using var suppliersTableCommand = new MySqlCommand(suppliersTableSql, connection);
        suppliersTableCommand.ExecuteNonQuery();

        EnsureColumnExists(connection, "Suppliers", "ContactInfo", "ALTER TABLE Suppliers ADD COLUMN ContactInfo VARCHAR(255) NULL;");

        // Old databases may contain supplier ids that don't exist in Suppliers yet.
        // Null them out before enforcing the foreign key so reads don't fail on startup.
        const string clearOrphanedSupplierIdsSql = @"UPDATE Products p
                                                     LEFT JOIN Suppliers s ON s.Id = p.SupplierId
                                                     SET p.SupplierId = NULL
                                                     WHERE p.SupplierId IS NOT NULL
                                                       AND s.Id IS NULL;";
        using var clearOrphanedSupplierIdsCommand = new MySqlCommand(clearOrphanedSupplierIdsSql, connection);
        clearOrphanedSupplierIdsCommand.ExecuteNonQuery();

        const string indexExistsSql = @"SELECT COUNT(*)
                                        FROM INFORMATION_SCHEMA.STATISTICS
                                        WHERE TABLE_SCHEMA = DATABASE()
                                          AND TABLE_NAME = 'Products'
                                          AND INDEX_NAME = 'IX_Products_SupplierId';";
        using var indexExistsCommand = new MySqlCommand(indexExistsSql, connection);
        int indexExists = Convert.ToInt32(indexExistsCommand.ExecuteScalar());
        if (indexExists == 0)
        {
            const string supplierIndexSql = @"CREATE INDEX IX_Products_SupplierId
                                              ON Products (SupplierId);";
            using var supplierIndexCommand = new MySqlCommand(supplierIndexSql, connection);
            supplierIndexCommand.ExecuteNonQuery();
        }

        const string fkExistsSql = @"SELECT COUNT(*)
                                     FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                                     WHERE CONSTRAINT_SCHEMA = DATABASE()
                                       AND TABLE_NAME = 'Products'
                                       AND CONSTRAINT_NAME = 'FK_Products_Suppliers'
                                       AND CONSTRAINT_TYPE = 'FOREIGN KEY';";
        using var fkExistsCommand = new MySqlCommand(fkExistsSql, connection);
        int fkExists = Convert.ToInt32(fkExistsCommand.ExecuteScalar());
        if (fkExists == 0)
        {
            const string addFkSql = @"ALTER TABLE Products
                                      ADD CONSTRAINT FK_Products_Suppliers
                                      FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id)
                                      ON DELETE SET NULL;";
            using var addFkCommand = new MySqlCommand(addFkSql, connection);
            addFkCommand.ExecuteNonQuery();
        }

        const string stockLogTableSql = @"CREATE TABLE IF NOT EXISTS StockLogs (
                                            Id INT AUTO_INCREMENT PRIMARY KEY,
                                            ProductId INT NOT NULL,
                                            PreviousQuantity INT NOT NULL,
                                            NewQuantity INT NOT NULL,
                                            ChangeAmount INT NOT NULL,
                                            Reason VARCHAR(255) NULL,
                                            UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                            INDEX IX_StockLogs_ProductId (ProductId),
                                            CONSTRAINT FK_StockLogs_Products
                                                FOREIGN KEY (ProductId) REFERENCES Products(Id)
                                                ON DELETE CASCADE
                                          );";

        using var stockLogCommand = new MySqlCommand(stockLogTableSql, connection);
        stockLogCommand.ExecuteNonQuery();
        _schemaInitialized = true;
        }
    }

    private static void EnsureColumnExists(MySqlConnection connection, string tableName, string columnName, string alterSql)
    {
        const string columnExistsSql = @"SELECT COUNT(*)
                                         FROM INFORMATION_SCHEMA.COLUMNS
                                         WHERE TABLE_SCHEMA = DATABASE()
                                           AND TABLE_NAME = @TableName
                                           AND COLUMN_NAME = @ColumnName;";

        using var columnExistsCommand = new MySqlCommand(columnExistsSql, connection);
        columnExistsCommand.Parameters.AddWithValue("@TableName", tableName);
        columnExistsCommand.Parameters.AddWithValue("@ColumnName", columnName);

        int columnExists = Convert.ToInt32(columnExistsCommand.ExecuteScalar());
        if (columnExists > 0)
        {
            return;
        }

        using var alterCommand = new MySqlCommand(alterSql, connection);
        alterCommand.ExecuteNonQuery();
    }
}
