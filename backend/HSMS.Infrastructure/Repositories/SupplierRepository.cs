using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using HSMS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace HSMS.Infrastructure.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly string _connectionString;

    public SupplierRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        EnsureSuppliersTableExists();
    }

    public async Task<List<Supplier>> GetSuppliersAsync()
    {
        var suppliers = new List<Supplier>();

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string query = @"SELECT Id, Name, ContactInfo, CreatedAt
                               FROM Suppliers
                               ORDER BY Name ASC";

        using var command = new MySqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            suppliers.Add(new Supplier
            {
                Id = Convert.ToInt32(reader["Id"]),
                Name = reader["Name"].ToString()!,
                ContactInfo = reader["ContactInfo"] == DBNull.Value ? null : reader["ContactInfo"].ToString(),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
            });
        }

        return suppliers;
    }

    public async Task<int> AddSupplierAsync(SupplierCreateDTO dto)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        // Check for duplicate supplier name
        const string checkDuplicateSql = "SELECT COUNT(*) FROM Suppliers WHERE LOWER(Name) = LOWER(@Name)";
        using (var checkCommand = new MySqlCommand(checkDuplicateSql, connection))
        {
            checkCommand.Parameters.AddWithValue("@Name", dto.Name);
            int count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
            if (count > 0)
            {
                throw new InvalidOperationException($"Supplier with name '{dto.Name}' already exists.");
            }
        }

        const string query = @"INSERT INTO Suppliers (Name, ContactInfo)
                               VALUES (@Name, @ContactInfo);
                               SELECT LAST_INSERT_ID();";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Name", dto.Name);
        command.Parameters.AddWithValue("@ContactInfo", (object?)dto.ContactInfo ?? DBNull.Value);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<bool> UpdateSupplierAsync(int id, SupplierUpdateDTO dto)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        // Check if supplier exists
        const string existsSql = "SELECT COUNT(*) FROM Suppliers WHERE Id = @Id";
        using (var existsCommand = new MySqlCommand(existsSql, connection))
        {
            existsCommand.Parameters.AddWithValue("@Id", id);
            int count = Convert.ToInt32(await existsCommand.ExecuteScalarAsync());
            if (count == 0)
            {
                return false;
            }
        }

        // Check for duplicate name (excluding current record)
        const string checkDuplicateSql = "SELECT COUNT(*) FROM Suppliers WHERE LOWER(Name) = LOWER(@Name) AND Id != @Id";
        using (var checkCommand = new MySqlCommand(checkDuplicateSql, connection))
        {
            checkCommand.Parameters.AddWithValue("@Name", dto.Name);
            checkCommand.Parameters.AddWithValue("@Id", id);
            int count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
            if (count > 0)
            {
                throw new InvalidOperationException($"Another supplier with name '{dto.Name}' already exists.");
            }
        }

        const string query = @"UPDATE Suppliers
                               SET Name = @Name, ContactInfo = @ContactInfo
                               WHERE Id = @Id";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Name", dto.Name);
        command.Parameters.AddWithValue("@ContactInfo", (object?)dto.ContactInfo ?? DBNull.Value);

        int rows = await command.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<SupplierDeleteStatus> DeleteSupplierAsync(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string existsSql = "SELECT COUNT(*) FROM Suppliers WHERE Id = @Id";
        using (var existsCommand = new MySqlCommand(existsSql, connection))
        {
            existsCommand.Parameters.AddWithValue("@Id", id);
            int count = Convert.ToInt32(await existsCommand.ExecuteScalarAsync());
            if (count == 0)
            {
                return SupplierDeleteStatus.NotFound;
            }
        }

        if (await HasLinkedRecordsAsync(connection, id))
        {
            return SupplierDeleteStatus.LinkedRecordsExist;
        }

        const string deleteSql = "DELETE FROM Suppliers WHERE Id = @Id";
        using var deleteCommand = new MySqlCommand(deleteSql, connection);
        deleteCommand.Parameters.AddWithValue("@Id", id);

        int rows = await deleteCommand.ExecuteNonQueryAsync();
        return rows > 0 ? SupplierDeleteStatus.Deleted : SupplierDeleteStatus.NotFound;
    }

    private static async Task<bool> HasLinkedRecordsAsync(MySqlConnection connection, int supplierId)
    {
        const string hasSupplierColumnSql = @"SELECT COUNT(*)
                                              FROM INFORMATION_SCHEMA.COLUMNS
                                              WHERE TABLE_SCHEMA = DATABASE()
                                                AND TABLE_NAME = 'Products'
                                                AND COLUMN_NAME = 'SupplierId'";

        using var hasColumnCommand = new MySqlCommand(hasSupplierColumnSql, connection);
        int hasColumn = Convert.ToInt32(await hasColumnCommand.ExecuteScalarAsync());
        if (hasColumn == 0)
        {
            return false;
        }

        const string linkedSql = "SELECT COUNT(*) FROM Products WHERE SupplierId = @SupplierId";
        using var linkedCommand = new MySqlCommand(linkedSql, connection);
        linkedCommand.Parameters.AddWithValue("@SupplierId", supplierId);
        int linkedCount = Convert.ToInt32(await linkedCommand.ExecuteScalarAsync());

        return linkedCount > 0;
    }

    private void EnsureSuppliersTableExists()
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        const string tableSql = @"CREATE TABLE IF NOT EXISTS Suppliers (
                                    Id INT AUTO_INCREMENT PRIMARY KEY,
                                    Name VARCHAR(255) NOT NULL UNIQUE,
                                    ContactInfo VARCHAR(255),
                                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                                  );";

        using var command = new MySqlCommand(tableSql, connection);
        command.ExecuteNonQuery();

        // Add ContactInfo column if it doesn't exist (migration support)
        const string alterTableSql = @"ALTER TABLE Suppliers
                                        ADD COLUMN IF NOT EXISTS ContactInfo VARCHAR(255);";

        using var alterCommand = new MySqlCommand(alterTableSql, connection);
        try
        {
            alterCommand.ExecuteNonQuery();
        }
        catch
        {
            // Column might already exist, safe to ignore
        }
    }
}
