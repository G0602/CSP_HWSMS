using HSMS.Domain.Entities;
using HSMS.Infrastructure.Data;
using MySql.Data.MySqlClient;
using System.Data;

namespace HSMS.Infrastructure.Repositories;

public class ProductRepository
{
    private readonly DbConnectionFactory _factory;

    public ProductRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        var products = new List<Product>();

        using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        var command = new MySqlCommand("SELECT * FROM Products", connection);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            products.Add(new Product
            {
                Id = reader.GetInt32("Id"),
                Name = reader.GetString("Name"),
                SKU = reader.GetString("SKU"),
                Price = reader.GetDecimal("Price"),
                Quantity = reader.GetInt32("Quantity"),
                Category = reader.GetString("Category")
            });
        }

        return products;
    }
}