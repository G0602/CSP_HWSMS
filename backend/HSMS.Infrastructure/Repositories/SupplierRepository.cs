using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
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
