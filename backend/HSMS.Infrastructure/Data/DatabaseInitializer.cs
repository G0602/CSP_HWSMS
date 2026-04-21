using MySql.Data.MySqlClient;

namespace HSMS.Infrastructure.Data;

public static class DatabaseInitializer
{
    private static readonly SemaphoreSlim InitializationLock = new(1, 1);
    private static bool _initialized;

    public static async Task InitializeAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        await InitializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            const string usersTableSql = @"CREATE TABLE IF NOT EXISTS Users (
                                            Id INT AUTO_INCREMENT PRIMARY KEY,
                                            Username VARCHAR(100) NOT NULL UNIQUE,
                                            PasswordHash VARCHAR(512) NOT NULL,
                                            Role VARCHAR(30) NOT NULL DEFAULT 'Cashier',
                                            CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                                          );";
            await ExecuteNonQueryAsync(connection, usersTableSql, cancellationToken);

            const string suppliersTableSql = @"CREATE TABLE IF NOT EXISTS Suppliers (
                                                Id INT AUTO_INCREMENT PRIMARY KEY,
                                                Name VARCHAR(255) NOT NULL UNIQUE,
                                                ContactInfo VARCHAR(255) NULL,
                                                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                                              );";
            await ExecuteNonQueryAsync(connection, suppliersTableSql, cancellationToken);
            await EnsureColumnExistsAsync(
                connection,
                "Suppliers",
                "ContactInfo",
                "ALTER TABLE Suppliers ADD COLUMN ContactInfo VARCHAR(255) NULL;",
                cancellationToken);

            const string productsTableSql = @"CREATE TABLE IF NOT EXISTS Products (
                                                Id INT AUTO_INCREMENT PRIMARY KEY,
                                                Name VARCHAR(255) NOT NULL,
                                                SKU VARCHAR(100) NOT NULL,
                                                Price DECIMAL(10,2) NOT NULL,
                                                Quantity INT NOT NULL,
                                                Category VARCHAR(255) NOT NULL,
                                                SupplierId INT NULL,
                                                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                                              );";
            await ExecuteNonQueryAsync(connection, productsTableSql, cancellationToken);

            await EnsureColumnExistsAsync(
                connection,
                "Products",
                "Price",
                "ALTER TABLE Products ADD COLUMN Price DECIMAL(10,2) NOT NULL DEFAULT 0;",
                cancellationToken);
            await EnsureColumnExistsAsync(
                connection,
                "Products",
                "Quantity",
                "ALTER TABLE Products ADD COLUMN Quantity INT NOT NULL DEFAULT 0;",
                cancellationToken);
            await EnsureColumnExistsAsync(
                connection,
                "Products",
                "Category",
                "ALTER TABLE Products ADD COLUMN Category VARCHAR(255) NOT NULL DEFAULT '';",
                cancellationToken);
            await EnsureColumnExistsAsync(
                connection,
                "Products",
                "SupplierId",
                "ALTER TABLE Products ADD COLUMN SupplierId INT NULL;",
                cancellationToken);

            const string clearOrphanedSupplierIdsSql = @"UPDATE Products p
                                                         LEFT JOIN Suppliers s ON s.Id = p.SupplierId
                                                         SET p.SupplierId = NULL
                                                         WHERE p.SupplierId IS NOT NULL
                                                           AND s.Id IS NULL;";
            await ExecuteNonQueryAsync(connection, clearOrphanedSupplierIdsSql, cancellationToken);

            await EnsureIndexExistsAsync(
                connection,
                "Products",
                "IX_Products_SupplierId",
                "CREATE INDEX IX_Products_SupplierId ON Products (SupplierId);",
                cancellationToken);
            await EnsureIndexExistsAsync(
                connection,
                "Products",
                "IX_Products_Quantity",
                "CREATE INDEX IX_Products_Quantity ON Products (Quantity);",
                cancellationToken);

            const string fkExistsSql = @"SELECT COUNT(*)
                                         FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                                         WHERE CONSTRAINT_SCHEMA = DATABASE()
                                           AND TABLE_NAME = 'Products'
                                           AND CONSTRAINT_NAME = 'FK_Products_Suppliers'
                                           AND CONSTRAINT_TYPE = 'FOREIGN KEY';";
            await using (var fkExistsCommand = new MySqlCommand(fkExistsSql, connection))
            {
                int fkExists = Convert.ToInt32(await fkExistsCommand.ExecuteScalarAsync(cancellationToken));
                if (fkExists == 0)
                {
                    const string addFkSql = @"ALTER TABLE Products
                                              ADD CONSTRAINT FK_Products_Suppliers
                                              FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id)
                                              ON DELETE SET NULL;";
                    await ExecuteNonQueryAsync(connection, addFkSql, cancellationToken);
                }
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
            await ExecuteNonQueryAsync(connection, stockLogTableSql, cancellationToken);

            const string salesTableSql = @"CREATE TABLE IF NOT EXISTS Sales (
                                             Id INT AUTO_INCREMENT PRIMARY KEY,
                                             SoldAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                             TotalAmount DECIMAL(10,2) NOT NULL,
                                             SoldBy VARCHAR(100) NOT NULL
                                           );";
            await ExecuteNonQueryAsync(connection, salesTableSql, cancellationToken);
            await EnsureIndexExistsAsync(
                connection,
                "Sales",
                "IX_Sales_SoldAt",
                "CREATE INDEX IX_Sales_SoldAt ON Sales (SoldAt);",
                cancellationToken);

            const string saleItemsTableSql = @"CREATE TABLE IF NOT EXISTS SaleItems (
                                                 Id INT AUTO_INCREMENT PRIMARY KEY,
                                                 SaleId INT NOT NULL,
                                                 ProductId INT NOT NULL,
                                                 ProductName VARCHAR(255) NOT NULL,
                                                 SKU VARCHAR(100) NOT NULL,
                                                 UnitPrice DECIMAL(10,2) NOT NULL,
                                                 Quantity INT NOT NULL,
                                                 LineSubtotal DECIMAL(10,2) NOT NULL,
                                                 FOREIGN KEY (SaleId) REFERENCES Sales(Id) ON DELETE CASCADE,
                                                 FOREIGN KEY (ProductId) REFERENCES Products(Id)
                                               );";
            await ExecuteNonQueryAsync(connection, saleItemsTableSql, cancellationToken);
            await EnsureIndexExistsAsync(
                connection,
                "SaleItems",
                "IX_SaleItems_SaleId",
                "CREATE INDEX IX_SaleItems_SaleId ON SaleItems (SaleId);",
                cancellationToken);
            await EnsureIndexExistsAsync(
                connection,
                "SaleItems",
                "IX_SaleItems_ProductId",
                "CREATE INDEX IX_SaleItems_ProductId ON SaleItems (ProductId);",
                cancellationToken);

            _initialized = true;
        }
        finally
        {
            InitializationLock.Release();
        }
    }

    private static async Task ExecuteNonQueryAsync(
        MySqlConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureColumnExistsAsync(
        MySqlConnection connection,
        string tableName,
        string columnName,
        string alterSql,
        CancellationToken cancellationToken)
    {
        const string columnExistsSql = @"SELECT COUNT(*)
                                         FROM INFORMATION_SCHEMA.COLUMNS
                                         WHERE TABLE_SCHEMA = DATABASE()
                                           AND TABLE_NAME = @TableName
                                           AND COLUMN_NAME = @ColumnName;";

        await using var columnExistsCommand = new MySqlCommand(columnExistsSql, connection);
        columnExistsCommand.Parameters.AddWithValue("@TableName", tableName);
        columnExistsCommand.Parameters.AddWithValue("@ColumnName", columnName);

        int columnExists = Convert.ToInt32(await columnExistsCommand.ExecuteScalarAsync(cancellationToken));
        if (columnExists > 0)
        {
            return;
        }

        await ExecuteNonQueryAsync(connection, alterSql, cancellationToken);
    }

    private static async Task EnsureIndexExistsAsync(
        MySqlConnection connection,
        string tableName,
        string indexName,
        string createSql,
        CancellationToken cancellationToken)
    {
        const string indexExistsSql = @"SELECT COUNT(*)
                                        FROM INFORMATION_SCHEMA.STATISTICS
                                        WHERE TABLE_SCHEMA = DATABASE()
                                          AND TABLE_NAME = @TableName
                                          AND INDEX_NAME = @IndexName;";

        await using var indexExistsCommand = new MySqlCommand(indexExistsSql, connection);
        indexExistsCommand.Parameters.AddWithValue("@TableName", tableName);
        indexExistsCommand.Parameters.AddWithValue("@IndexName", indexName);

        int indexExists = Convert.ToInt32(await indexExistsCommand.ExecuteScalarAsync(cancellationToken));
        if (indexExists > 0)
        {
            return;
        }

        await ExecuteNonQueryAsync(connection, createSql, cancellationToken);
    }
}
