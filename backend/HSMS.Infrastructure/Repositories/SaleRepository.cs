using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using HSMS.Application.Services;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace HSMS.Infrastructure.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly string _connectionString;

    public SaleRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        EnsureSalesTablesExist();
    }

    public async Task<SaleResponseDTO> CreateSaleAsync(SaleCreateDTO sale, string soldBy)
    {
        if (sale.Items.Count == 0)
        {
            throw new InvalidOperationException("At least one sale item is required.");
        }

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var processedItems = new List<SaleItemResponseDTO>();

            foreach (var item in sale.Items)
            {
                if (item.Quantity <= 0)
                {
                    throw new InvalidOperationException("Item quantity must be greater than zero.");
                }

                const string productQuery = @"SELECT Id, Name, SKU, Price, Quantity
                                              FROM Products
                                              WHERE Id = @ProductId
                                              FOR UPDATE";

                await using var productCommand = new MySqlCommand(productQuery, connection, (MySqlTransaction)transaction);
                productCommand.Parameters.AddWithValue("@ProductId", item.ProductId);

                await using var reader = await productCommand.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    throw new InvalidOperationException($"Product {item.ProductId} was not found.");
                }

                int availableQuantity = Convert.ToInt32(reader["Quantity"]);
                decimal unitPrice = Convert.ToDecimal(reader["Price"]);
                string productName = reader["Name"].ToString()!;
                string sku = reader["SKU"].ToString()!;

                if (availableQuantity < item.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for {productName}. Available: {availableQuantity}.");
                }

                await reader.DisposeAsync();

                decimal lineSubtotal = SaleCalculator.CalculateLineSubtotal(unitPrice, item.Quantity);

                processedItems.Add(new SaleItemResponseDTO
                {
                    ProductId = item.ProductId,
                    ProductName = productName,
                    SKU = sku,
                    UnitPrice = unitPrice,
                    Quantity = item.Quantity,
                    LineSubtotal = lineSubtotal
                });
            }

            decimal totalAmount = SaleCalculator.CalculateTotal(processedItems.Select(i => i.LineSubtotal));

            const string saleInsert = @"INSERT INTO Sales (TotalAmount, SoldBy)
                                        VALUES (@TotalAmount, @SoldBy);
                                        SELECT LAST_INSERT_ID();";

            await using var saleCommand = new MySqlCommand(saleInsert, connection, (MySqlTransaction)transaction);
            saleCommand.Parameters.AddWithValue("@TotalAmount", totalAmount);
            saleCommand.Parameters.AddWithValue("@SoldBy", soldBy);

            object? saleResult = await saleCommand.ExecuteScalarAsync();
            int saleId = Convert.ToInt32(saleResult);

            foreach (var item in processedItems)
            {
                const string saleItemInsert = @"INSERT INTO SaleItems (SaleId, ProductId, ProductName, SKU, UnitPrice, Quantity, LineSubtotal)
                                                VALUES (@SaleId, @ProductId, @ProductName, @SKU, @UnitPrice, @Quantity, @LineSubtotal);";

                await using var itemCommand = new MySqlCommand(saleItemInsert, connection, (MySqlTransaction)transaction);
                itemCommand.Parameters.AddWithValue("@SaleId", saleId);
                itemCommand.Parameters.AddWithValue("@ProductId", item.ProductId);
                itemCommand.Parameters.AddWithValue("@ProductName", item.ProductName);
                itemCommand.Parameters.AddWithValue("@SKU", item.SKU);
                itemCommand.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                itemCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                itemCommand.Parameters.AddWithValue("@LineSubtotal", item.LineSubtotal);
                await itemCommand.ExecuteNonQueryAsync();

                const string stockUpdate = @"UPDATE Products
                                             SET Quantity = Quantity - @Qty
                                             WHERE Id = @ProductId
                                               AND Quantity >= @Qty";

                await using var updateCommand = new MySqlCommand(stockUpdate, connection, (MySqlTransaction)transaction);
                updateCommand.Parameters.AddWithValue("@Qty", item.Quantity);
                updateCommand.Parameters.AddWithValue("@ProductId", item.ProductId);
                int updatedRows = await updateCommand.ExecuteNonQueryAsync();
                if (updatedRows == 0)
                {
                    throw new InvalidOperationException($"Stock update failed for product {item.ProductId}.");
                }
            }

            await transaction.CommitAsync();

            return new SaleResponseDTO
            {
                SaleId = saleId,
                SoldAt = DateTime.UtcNow,
                TotalAmount = totalAmount,
                SoldBy = soldBy,
                Items = processedItems
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private void EnsureSalesTablesExist()
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

                const string productsTableSql = @"CREATE TABLE IF NOT EXISTS Products (
                                                                                        Id INT AUTO_INCREMENT PRIMARY KEY,
                                                                                        Name VARCHAR(255) NOT NULL,
                                                                                        SKU VARCHAR(100) NOT NULL,
                                                                                        Price DECIMAL(10,2) NOT NULL,
                                                                                        Quantity INT NOT NULL,
                                                                                        Category VARCHAR(255) NOT NULL,
                                                                                        CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                                                                                    );";

        const string salesTableSql = @"CREATE TABLE IF NOT EXISTS Sales (
                                         Id INT AUTO_INCREMENT PRIMARY KEY,
                                         SoldAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                         TotalAmount DECIMAL(10,2) NOT NULL,
                                         SoldBy VARCHAR(100) NOT NULL
                                       );";

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

        using var productsCommand = new MySqlCommand(productsTableSql, connection);
        productsCommand.ExecuteNonQuery();

        using var salesCommand = new MySqlCommand(salesTableSql, connection);
        salesCommand.ExecuteNonQuery();

        using var saleItemsCommand = new MySqlCommand(saleItemsTableSql, connection);
        saleItemsCommand.ExecuteNonQuery();
    }
}
