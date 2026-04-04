using HSMS.Application.Interfaces;
using HSMS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace HSMS.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        EnsureUsersTableExists();
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string query = @"SELECT Id, Username, PasswordHash, Role, CreatedAt
                               FROM Users
                               WHERE Username = @Username
                               LIMIT 1";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Username", username);

        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new User
        {
            Id = Convert.ToInt32(reader["Id"]),
            Username = reader["Username"].ToString()!,
            PasswordHash = reader["PasswordHash"].ToString()!,
            Role = reader["Role"].ToString()!,
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
        };
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string query = @"SELECT Id, Username, PasswordHash, Role, CreatedAt
                               FROM Users
                               WHERE Id = @Id
                               LIMIT 1";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Id", id);

        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new User
        {
            Id = Convert.ToInt32(reader["Id"]),
            Username = reader["Username"].ToString()!,
            PasswordHash = reader["PasswordHash"].ToString()!,
            Role = reader["Role"].ToString()!,
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
        };
    }

    public async Task<int> CreateUserAsync(string username, string passwordHash, string role)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string query = @"INSERT INTO Users (Username, PasswordHash, Role)
                               VALUES (@Username, @PasswordHash, @Role);
                               SELECT LAST_INSERT_ID();";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Username", username);
        command.Parameters.AddWithValue("@PasswordHash", passwordHash);
        command.Parameters.AddWithValue("@Role", role);

        object? result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<bool> UpdateRoleAsync(int id, string role)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string query = @"UPDATE Users
                               SET Role = @Role
                               WHERE Id = @Id";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Role", role);
        command.Parameters.AddWithValue("@Id", id);

        int rows = await command.ExecuteNonQueryAsync();
        return rows > 0;
    }

    private void EnsureUsersTableExists()
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        const string tableSql = @"CREATE TABLE IF NOT EXISTS Users (
                                    Id INT AUTO_INCREMENT PRIMARY KEY,
                                    Username VARCHAR(100) NOT NULL UNIQUE,
                                    PasswordHash VARCHAR(512) NOT NULL,
                                                                        Role VARCHAR(30) NOT NULL DEFAULT 'Cashier',
                                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                                  );";

        using var command = new MySqlCommand(tableSql, connection);
        command.ExecuteNonQuery();
    }
}
