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

    public async Task<List<SaleHistoryItemDTO>> GetSalesHistoryAsync(int? saleId, DateTime? fromDate, DateTime? toDate, int limit = 100)
    {
        var history = new List<SaleHistoryItemDTO>();

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var filters = new List<string>();
        if (saleId.HasValue)
        {
            filters.Add("s.Id = @SaleId");
        }

        if (fromDate.HasValue)
        {
            filters.Add("s.SoldAt >= @FromDate");
        }

        if (toDate.HasValue)
        {
            filters.Add("s.SoldAt < @ToDateExclusive");
        }

        string whereClause = filters.Count > 0 ? $"WHERE {string.Join(" AND ", filters)}" : string.Empty;

        string query = $@"SELECT s.Id, s.SoldAt, s.TotalAmount, s.SoldBy, COUNT(si.Id) AS ItemCount
                          FROM Sales s
                          LEFT JOIN SaleItems si ON si.SaleId = s.Id
                          {whereClause}
                          GROUP BY s.Id, s.SoldAt, s.TotalAmount, s.SoldBy
                          ORDER BY s.SoldAt DESC
                          LIMIT @Limit";

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Limit", Math.Clamp(limit, 1, 500));

        if (saleId.HasValue)
        {
            command.Parameters.AddWithValue("@SaleId", saleId.Value);
        }

        if (fromDate.HasValue)
        {
            command.Parameters.AddWithValue("@FromDate", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            command.Parameters.AddWithValue("@ToDateExclusive", toDate.Value.Date.AddDays(1));
        }

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            history.Add(new SaleHistoryItemDTO
            {
                SaleId = Convert.ToInt32(reader["Id"]),
                SoldAt = Convert.ToDateTime(reader["SoldAt"]),
                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                SoldBy = reader["SoldBy"].ToString()!,
                ItemCount = Convert.ToInt32(reader["ItemCount"]),
            });
        }

        return history;
    }

    public async Task<SaleResponseDTO?> GetSaleDetailsAsync(int saleId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string saleQuery = @"SELECT Id, SoldAt, TotalAmount, SoldBy
                                   FROM Sales
                                   WHERE Id = @SaleId";

        await using var saleCommand = new MySqlCommand(saleQuery, connection);
        saleCommand.Parameters.AddWithValue("@SaleId", saleId);

        await using var saleReader = await saleCommand.ExecuteReaderAsync();
        if (!await saleReader.ReadAsync())
        {
            return null;
        }

        var sale = new SaleResponseDTO
        {
            SaleId = Convert.ToInt32(saleReader["Id"]),
            SoldAt = Convert.ToDateTime(saleReader["SoldAt"]),
            TotalAmount = Convert.ToDecimal(saleReader["TotalAmount"]),
            SoldBy = saleReader["SoldBy"].ToString()!,
            Items = new List<SaleItemResponseDTO>(),
        };

        await saleReader.DisposeAsync();

        const string itemsQuery = @"SELECT ProductId, ProductName, SKU, UnitPrice, Quantity, LineSubtotal
                                    FROM SaleItems
                                    WHERE SaleId = @SaleId
                                    ORDER BY Id ASC";

        await using var itemsCommand = new MySqlCommand(itemsQuery, connection);
        itemsCommand.Parameters.AddWithValue("@SaleId", saleId);

        await using var itemsReader = await itemsCommand.ExecuteReaderAsync();
        while (await itemsReader.ReadAsync())
        {
            sale.Items.Add(new SaleItemResponseDTO
            {
                ProductId = Convert.ToInt32(itemsReader["ProductId"]),
                ProductName = itemsReader["ProductName"].ToString()!,
                SKU = itemsReader["SKU"].ToString()!,
                UnitPrice = Convert.ToDecimal(itemsReader["UnitPrice"]),
                Quantity = Convert.ToInt32(itemsReader["Quantity"]),
                LineSubtotal = Convert.ToDecimal(itemsReader["LineSubtotal"]),
            });
        }

        return sale;
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
