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

    public async Task<List<DailySalesReportItemDTO>> GetDailySalesReportAsync()
    {
        var report = new List<DailySalesReportItemDTO>();

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string query = @"SELECT DATE(SoldAt) AS ReportDate,
                                      SUM(TotalAmount) AS TotalAmount
                               FROM Sales
                       GROUP BY DATE(SoldAt)
                               ORDER BY ReportDate DESC";

        await using var command = new MySqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            report.Add(new DailySalesReportItemDTO
            {
                Date = Convert.ToDateTime(reader["ReportDate"]),
                TotalAmount = Convert.ToDecimal(reader["TotalAmount"])
            });
        }

        return report;
    }

    public async Task<List<MonthlySalesReportItemDTO>> GetMonthlySalesReportAsync()
    {
        var report = new List<MonthlySalesReportItemDTO>();

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string query = @"SELECT DATE_SUB(DATE(SoldAt), INTERVAL DAY(SoldAt) - 1 DAY) AS ReportMonth,
                                      SUM(TotalAmount) AS TotalAmount
                               FROM Sales
                       GROUP BY DATE_SUB(DATE(SoldAt), INTERVAL DAY(SoldAt) - 1 DAY)
                               ORDER BY ReportMonth DESC";

        await using var command = new MySqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            report.Add(new MonthlySalesReportItemDTO
            {
                Month = Convert.ToDateTime(reader["ReportMonth"]),
                TotalAmount = Convert.ToDecimal(reader["TotalAmount"])
            });
        }

        return report;
    }

    public async Task<SalesAnalyticsResponseDTO> GetSalesAnalyticsAsync(
        DateTime? fromDate,
        DateTime? toDate,
        int? productId,
        string? category,
        decimal costRatio)
    {
        decimal normalizedCostRatio = decimal.Clamp(costRatio, 0m, 1m);
        string normalizedCategory = category?.Trim() ?? string.Empty;

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var filterClauses = new List<string>();
        if (fromDate.HasValue)
        {
            filterClauses.Add("s.SoldAt >= @FromDate");
        }

        if (toDate.HasValue)
        {
            filterClauses.Add("s.SoldAt < @ToDateExclusive");
        }

        if (productId.HasValue)
        {
            filterClauses.Add("si.ProductId = @ProductId");
        }

        if (!string.IsNullOrWhiteSpace(normalizedCategory))
        {
            filterClauses.Add("p.Category = @Category");
        }

        string whereClause = filterClauses.Count > 0
            ? $"WHERE {string.Join(" AND ", filterClauses)}"
            : string.Empty;

        string fromAndJoins = $@"FROM SaleItems si
                                 INNER JOIN Sales s ON s.Id = si.SaleId
                                 LEFT JOIN Products p ON p.Id = si.ProductId
                                 {whereClause}";

        async Task AddCommonParametersAsync(MySqlCommand command)
        {
            command.Parameters.AddWithValue("@CostRatio", normalizedCostRatio);

            if (fromDate.HasValue)
            {
                command.Parameters.AddWithValue("@FromDate", fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                command.Parameters.AddWithValue("@ToDateExclusive", toDate.Value.Date.AddDays(1));
            }

            if (productId.HasValue)
            {
                command.Parameters.AddWithValue("@ProductId", productId.Value);
            }

            if (!string.IsNullOrWhiteSpace(normalizedCategory))
            {
                command.Parameters.AddWithValue("@Category", normalizedCategory);
            }

            await Task.CompletedTask;
        }

        var response = new SalesAnalyticsResponseDTO();

        string totalsQuery = $@"SELECT
                                    COALESCE(SUM(si.LineSubtotal), 0) AS TotalSales,
                                    ROUND(COALESCE(SUM(si.LineSubtotal), 0) * @CostRatio, 2) AS TotalCost,
                                    ROUND(COALESCE(SUM(si.LineSubtotal), 0) - (COALESCE(SUM(si.LineSubtotal), 0) * @CostRatio), 2) AS TotalProfit
                                {fromAndJoins};";

        await using (var totalsCommand = new MySqlCommand(totalsQuery, connection))
        {
            await AddCommonParametersAsync(totalsCommand);
            await using var reader = await totalsCommand.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                response.TotalSales = Convert.ToDecimal(reader["TotalSales"]);
                response.TotalCost = Convert.ToDecimal(reader["TotalCost"]);
                response.TotalProfit = Convert.ToDecimal(reader["TotalProfit"]);
            }
        }

        string dailyQuery = $@"SELECT
                                   DATE(s.SoldAt) AS ReportDate,
                                   COALESCE(SUM(si.LineSubtotal), 0) AS Sales,
                                   ROUND(COALESCE(SUM(si.LineSubtotal), 0) * @CostRatio, 2) AS Cost,
                                   ROUND(COALESCE(SUM(si.LineSubtotal), 0) - (COALESCE(SUM(si.LineSubtotal), 0) * @CostRatio), 2) AS Profit
                               {fromAndJoins}
                               GROUP BY DATE(s.SoldAt)
                               ORDER BY ReportDate ASC;";

        await using (var dailyCommand = new MySqlCommand(dailyQuery, connection))
        {
            await AddCommonParametersAsync(dailyCommand);
            await using var reader = await dailyCommand.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                response.DailyTrends.Add(new DailySalesAnalyticsItemDTO
                {
                    Date = Convert.ToDateTime(reader["ReportDate"]),
                    Sales = Convert.ToDecimal(reader["Sales"]),
                    Cost = Convert.ToDecimal(reader["Cost"]),
                    Profit = Convert.ToDecimal(reader["Profit"])
                });
            }
        }

        string monthlyQuery = $@"SELECT
                                     DATE_SUB(DATE(s.SoldAt), INTERVAL DAY(s.SoldAt) - 1 DAY) AS ReportMonth,
                                     COALESCE(SUM(si.LineSubtotal), 0) AS Sales,
                                     ROUND(COALESCE(SUM(si.LineSubtotal), 0) * @CostRatio, 2) AS Cost,
                                     ROUND(COALESCE(SUM(si.LineSubtotal), 0) - (COALESCE(SUM(si.LineSubtotal), 0) * @CostRatio), 2) AS Profit
                                 {fromAndJoins}
                                 GROUP BY DATE_SUB(DATE(s.SoldAt), INTERVAL DAY(s.SoldAt) - 1 DAY)
                                 ORDER BY ReportMonth ASC;";

        await using (var monthlyCommand = new MySqlCommand(monthlyQuery, connection))
        {
            await AddCommonParametersAsync(monthlyCommand);
            await using var reader = await monthlyCommand.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                response.MonthlyTrends.Add(new MonthlySalesAnalyticsItemDTO
                {
                    Month = Convert.ToDateTime(reader["ReportMonth"]),
                    Sales = Convert.ToDecimal(reader["Sales"]),
                    Cost = Convert.ToDecimal(reader["Cost"]),
                    Profit = Convert.ToDecimal(reader["Profit"])
                });
            }
        }

        return response;
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

    public async Task<InvoiceResponseDTO?> GetInvoiceAsync(int saleId)
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

        var invoice = new InvoiceResponseDTO
        {
            TransactionId = Convert.ToInt32(saleReader["Id"]),
            InvoiceNumber = $"INV-{Convert.ToInt32(saleReader["Id"]):D6}",
            SoldAt = Convert.ToDateTime(saleReader["SoldAt"]),
            SoldBy = saleReader["SoldBy"].ToString()!,
            GrandTotal = Convert.ToDecimal(saleReader["TotalAmount"]),
            TaxRate = 0m,
            Items = new List<InvoiceItemDTO>(),
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
            invoice.Items.Add(new InvoiceItemDTO
            {
                ProductId = Convert.ToInt32(itemsReader["ProductId"]),
                ProductName = itemsReader["ProductName"].ToString()!,
                SKU = itemsReader["SKU"].ToString()!,
                UnitPrice = Convert.ToDecimal(itemsReader["UnitPrice"]),
                Quantity = Convert.ToInt32(itemsReader["Quantity"]),
                LineSubtotal = Convert.ToDecimal(itemsReader["LineSubtotal"]),
            });
        }

        invoice.Subtotal = invoice.Items.Sum(i => i.LineSubtotal);
        invoice.TaxAmount = invoice.GrandTotal - invoice.Subtotal;

        // Prevent negative tax from precision inconsistencies and preserve DB grand total as source of truth.
        if (invoice.TaxAmount < 0)
        {
            invoice.TaxAmount = 0;
            invoice.Subtotal = invoice.GrandTotal;
        }

        return invoice;
    }
}
