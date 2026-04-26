using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using HSMS.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace HSMS.Tests;

/// <summary>
/// Tests for SQL injection protection and search parameter escaping
/// Verifies that special SQL characters are properly escaped
/// Requires: HSMS_TEST_CONNECTION_STRING environment variable
/// </summary>
[Collection("DatabaseIntegration")]
public class SqlInjectionProtectionTests
{
    private static string? GetConnectionString() => System.Environment.GetEnvironmentVariable("HSMS_TEST_CONNECTION_STRING");

    private static ProductRepository CreateRepository(string connectionString)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", connectionString) })
            .Build();
        return new ProductRepository(config);
    }

    [Theory]
    [InlineData("'; DROP TABLE products; --")]
    [InlineData("1' OR '1'='1")]
    [InlineData("admin'--")]
    [InlineData("' OR 1=1 --")]
    [InlineData("'; DELETE FROM products WHERE '1'='1")]
    public async Task SearchProducts_Should_Escape_Sql_Injection_Attempts(string injectionPayload)
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var repository = CreateRepository(connectionString);

        // Setup: Create a test product
        var productId = await repository.AddProduct(new ProductCreateDTO
        {
            Name = "Test Product",
            SKU = "TEST-001",
            Category = "Testing",
            Price = 100m,
            Quantity = 10
        });

        // Execute: Try to search with injection payload
        var results = await repository.SearchProducts(injectionPayload, limit: 100);

        // Verify: Should return empty results or safe results, not cause error or table deletion
        Assert.NotNull(results);
        // Products table should still exist and have our test product
        var products = await repository.GetAllProducts();
        Assert.Contains(products, p => p.Id == productId);

        // Cleanup
        await repository.DeleteProduct(productId);
    }

    [Theory]
    [InlineData("%")]     // LIKE wildcard
    [InlineData("_")]     // LIKE single char
    [InlineData("[")]     // Regex character
    [InlineData("]")]     // Regex character
    [InlineData("^")]     // Regex anchor
    public async Task SearchProducts_Should_Handle_Sql_Like_Wildcards(string sqlWildcard)
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var repository = CreateRepository(connectionString);

        // Setup
        var productId = await repository.AddProduct(new ProductCreateDTO
        {
            Name = "Product With % Symbol",
            SKU = "PCT-001",
            Category = "Testing",
            Price = 100m,
            Quantity = 10
        });

        // Execute: Search with wildcard
        var results = await repository.SearchProducts(sqlWildcard, limit: 100);

        // Verify: Should not throw exception and return valid results
        Assert.NotNull(results);

        // Cleanup
        await repository.DeleteProduct(productId);
    }

    [Fact]
    public async Task SearchProducts_Should_Find_Product_With_Special_Characters_In_Name()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var repository = CreateRepository(connectionString);

        // Setup: Create product with special characters
        var productId = await repository.AddProduct(new ProductCreateDTO
        {
            Name = "Product & Services (Premium) 50%",
            SKU = "SPC-001",
            Category = "Special",
            Price = 1500.99m,
            Quantity = 25
        });

        // Execute: Search for product with special chars
        var results = await repository.SearchProducts("Product & Services", limit: 100);

        // Verify: Should find the product
        Assert.NotNull(results);
        Assert.Contains(results, p => p.Id == productId);

        // Cleanup
        await repository.DeleteProduct(productId);
    }

    [Theory]
    [InlineData("Product' OR '1'='1")]
    [InlineData("Product\" OR \"1\"=\"1")]
    [InlineData("Product`; DELETE")]
    [InlineData("Product'; DROP--")]
    public async Task SearchProducts_Should_Escape_Quote_Characters(string queryWithQuotes)
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var repository = CreateRepository(connectionString);

        // Setup
        var productId = await repository.AddProduct(new ProductCreateDTO
        {
            Name = "Regular Product",
            SKU = "REG-001",
            Category = "Testing",
            Price = 100m,
            Quantity = 10
        });

        // Execute: Search with quote injection
        var results = await repository.SearchProducts(queryWithQuotes, limit: 100);

        // Verify: Should not throw and should not find products with injection attempt
        Assert.NotNull(results);
        // Verify table integrity - our product should still be there
        var allProducts = await repository.GetAllProducts();
        Assert.Contains(allProducts, p => p.Id == productId);

        // Cleanup
        await repository.DeleteProduct(productId);
    }

    [Fact]
    public async Task SearchProducts_Should_Find_Product_By_Sku_With_Special_Characters()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var repository = CreateRepository(connectionString);

        // Setup
        var productId = await repository.AddProduct(new ProductCreateDTO
        {
            Name = "Special SKU Product",
            SKU = "SKU-2024-ABC-123",
            Category = "Testing",
            Price = 500m,
            Quantity = 5
        });

        // Execute: Search by SKU with hyphens
        var results = await repository.SearchProducts("SKU-2024", limit: 100);

        // Verify: Should find product
        Assert.NotNull(results);
        Assert.Contains(results, p => p.Id == productId);

        // Cleanup
        await repository.DeleteProduct(productId);
    }

    [Fact]
    public async Task SearchProducts_Should_Find_Product_By_Category_With_Special_Characters()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var repository = CreateRepository(connectionString);

        // Setup
        var productId = await repository.AddProduct(new ProductCreateDTO
        {
            Name = "Category Search Product",
            SKU = "CAT-001",
            Category = "Tools & Accessories (Premium)",
            Price = 750m,
            Quantity = 15
        });

        // Execute: Search by category with special chars
        var results = await repository.SearchProducts("Tools & Accessories", limit: 100);

        // Verify: Should find product
        Assert.NotNull(results);
        Assert.Contains(results, p => p.Id == productId);

        // Cleanup
        await repository.DeleteProduct(productId);
    }

    [Theory]
    [InlineData("")]           // Empty string
    [InlineData("   ")]        // Only whitespace
    [InlineData(null)]         // Null (if supported)
    public async Task SearchProducts_Should_Handle_Empty_Or_Null_Queries(string? query)
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var repository = CreateRepository(connectionString);

        // Setup
        var productId = await repository.AddProduct(new ProductCreateDTO
        {
            Name = "Empty Query Test",
            SKU = "EQ-001",
            Category = "Testing",
            Price = 100m,
            Quantity = 10
        });

        // Execute: Search with empty/null
        if (query != null)
        {
            var results = await repository.SearchProducts(query, limit: 100);
            // Should handle gracefully - return all or empty
            Assert.NotNull(results);
        }

        // Cleanup
        await repository.DeleteProduct(productId);
    }

    [Fact]
    public async Task SearchProducts_Should_Respect_Limit_Parameter()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var repository = CreateRepository(connectionString);

        // Setup: Create 15 test products
        var productIds = new List<int>();
        for (int i = 0; i < 15; i++)
        {
            var id = await repository.AddProduct(new ProductCreateDTO
            {
                Name = $"Test Product {i}",
                SKU = $"LIMIT-{i:00}",
                Category = "Testing",
                Price = 100m,
                Quantity = 10
            });
            productIds.Add(id);
        }

        // Execute: Search with different limits
        var limit5Results = await repository.SearchProducts("Test Product", limit: 5);
        var limit10Results = await repository.SearchProducts("Test Product", limit: 10);

        // Verify: Results should respect limit
        Assert.True(limit5Results.Count <= 5);
        Assert.True(limit10Results.Count <= 10);

        // Cleanup
        foreach (var id in productIds)
        {
            await repository.DeleteProduct(id);
        }
    }

    [Fact]
    public async Task SearchProducts_Should_Not_Return_Deleted_Products()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var repository = CreateRepository(connectionString);

        // Setup: Create and delete a product
        var productId = await repository.AddProduct(new ProductCreateDTO
        {
            Name = "Deleted Search Product",
            SKU = "DEL-001",
            Category = "Testing",
            Price = 100m,
            Quantity = 10
        });

        await repository.DeleteProduct(productId);

        // Execute: Search for deleted product
        var results = await repository.SearchProducts("Deleted Search", limit: 100);

        // Verify: Deleted product should not appear
        Assert.NotNull(results);
        Assert.DoesNotContain(results, p => p.Id == productId);
    }
}
