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

        const string query = @"SELECT Id, Name, CreatedAt
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
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
            });
        }

        return suppliers;
    }

    public async Task<int> AddSupplierAsync(SupplierCreateDTO dto)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string query = @"INSERT INTO Suppliers (Name)
                               VALUES (@Name);
                               SELECT LAST_INSERT_ID();";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Name", dto.Name);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<bool> UpdateSupplierAsync(int id, SupplierUpdateDTO dto)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string query = @"UPDATE Suppliers
                               SET Name = @Name
                               WHERE Id = @Id";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Name", dto.Name);

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
                                    Name VARCHAR(255) NOT NULL,
                                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                                  );";

        using var command = new MySqlCommand(tableSql, connection);
        command.ExecuteNonQuery();
    }
}
