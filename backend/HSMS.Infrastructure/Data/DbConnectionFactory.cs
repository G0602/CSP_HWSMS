using MySql.Data.MySqlClient;

namespace HSMS.Infrastructure.Data;

/// <summary>
/// Factory that creates open-able <see cref="MySqlConnection"/> instances
/// from a pre-configured connection string.
/// Centralises connection creation so the string is never scattered across the codebase.
/// </summary>
public class DbConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>
    /// Initialises the factory with a MySQL connection string.
    /// </summary>
    /// <param name="connectionString">
    /// A valid MySQL connection string
    /// (e.g. <c>server=localhost;database=CSP_HSMS;user=CSP;password=***;</c>).
    /// </param>
    public DbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Creates and returns a new (un-opened) <see cref="MySqlConnection"/>.
    /// The caller is responsible for opening and disposing the connection.
    /// </summary>
    /// <returns>A new <see cref="MySqlConnection"/> configured with the stored connection string.</returns>
    public MySqlConnection CreateConnection()
    {
        return new MySqlConnection(_connectionString);
    }
}