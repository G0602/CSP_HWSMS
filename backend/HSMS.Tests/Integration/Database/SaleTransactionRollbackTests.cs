using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using HSMS.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace HSMS.Tests;

/// <summary>
/// Tests for sale transaction rollback scenarios and error handling
/// Verifies that failed sales don't leave data in inconsistent state
/// Requires: HSMS_TEST_CONNECTION_STRING environment variable
/// </summary>
[Collection("DatabaseIntegration")]
public class SaleTransactionRollbackTests
{
    private static string? GetConnectionString() => System.Environment.GetEnvironmentVariable("HSMS_TEST_CONNECTION_STRING");

    private static SaleRepository CreateSaleRepository(string connectionString)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", connectionString) })
            .Build();
        return new SaleRepository(config);
    }

    private static ProductRepository CreateProductRepository(string connectionString)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", connectionString) })
            .Build();
        return new ProductRepository(config);
    }

    [Fact]
    public async Task CreateSale_Should_Fail_When_Items_Collection_Is_Empty()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var saleRepository = CreateSaleRepository(connectionString);

        var saleDto = new SaleCreateDTO
        {
            Items = new List<SaleItemCreateDTO>() // Empty items
        };

        // Execute & Verify: Should throw ArgumentException
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            saleRepository.CreateSaleAsync(saleDto, "cashier1"));
        
        Assert.Contains("items", exception.Message.ToLower());
    }

    [Fact]
    public async Task CreateSale_Should_Fail_When_Item_Quantity_Is_Zero()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var saleRepository = CreateSaleRepository(connectionString);
        var productRepository = CreateProductRepository(connectionString);

        // Setup: Create a product
        var productId = await productRepository.AddProduct(new ProductCreateDTO
        {
            Name = "Test Product",
            SKU = "TST-001",
            Category = "Testing",
            Price = 100m,
            Quantity = 100
        });

        var saleDto = new SaleCreateDTO
        {
            Items = new List<SaleItemCreateDTO>
            {
                new SaleItemCreateDTO
                {
                    ProductId = productId,
                    Quantity = 0 // Invalid: zero quantity
                }
            }
        };

        // Execute & Verify: Should throw ArgumentException
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            saleRepository.CreateSaleAsync(saleDto, "cashier1"));

        Assert.Contains("quantity", exception.Message.ToLower());

        // Cleanup
        await productRepository.DeleteProduct(productId);
    }

    [Fact]
    public async Task CreateSale_Should_Fail_When_Item_Quantity_Is_Negative()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var saleRepository = CreateSaleRepository(connectionString);
        var productRepository = CreateProductRepository(connectionString);

        // Setup
        var productId = await productRepository.AddProduct(new ProductCreateDTO
        {
            Name = "Negative Qty Test",
            SKU = "NEG-001",
            Category = "Testing",
            Price = 100m,
            Quantity = 100
        });

        var saleDto = new SaleCreateDTO
        {
            Items = new List<SaleItemCreateDTO>
            {
                new SaleItemCreateDTO
                {
                    ProductId = productId,
                    Quantity = -5 // Negative quantity
                }
            }
        };

        // Execute & Verify: Should throw ArgumentException
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            saleRepository.CreateSaleAsync(saleDto, "cashier1"));

        // Cleanup
        await productRepository.DeleteProduct(productId);
    }

    [Fact]
    public async Task CreateSale_Should_Fail_When_Any_Item_Has_Insufficient_Stock()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var saleRepository = CreateSaleRepository(connectionString);
        var productRepository = CreateProductRepository(connectionString);

        // Setup: Create two products, second one has low stock
        var product1Id = await productRepository.AddProduct(new ProductCreateDTO
        {
            Name = "Product 1",
            SKU = "P1-001",
            Category = "Testing",
            Price = 100m,
            Quantity = 50
        });

        var product2Id = await productRepository.AddProduct(new ProductCreateDTO
        {
            Name = "Product 2",
            SKU = "P2-001",
            Category = "Testing",
            Price = 200m,
            Quantity = 5 // Only 5 in stock
        });

        var saleDto = new SaleCreateDTO
        {
            Items = new List<SaleItemCreateDTO>
            {
                new SaleItemCreateDTO { ProductId = product1Id, Quantity = 10 }, // OK
                new SaleItemCreateDTO { ProductId = product2Id, Quantity = 10 }  // Exceeds stock!
            }
        };

        // Execute & Verify: Should throw exception for insufficient stock
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            saleRepository.CreateSaleAsync(saleDto, "cashier1"));

        Assert.Contains("insufficient", exception.Message, System.StringComparison.OrdinalIgnoreCase);

        // Verify: Stock should NOT be deducted from product1
        var product1 = await productRepository.GetProductById(product1Id);
        Assert.NotNull(product1);
        Assert.Equal(50, product1.Quantity); // Should still be 50, not 40

        // Verify: Stock should NOT be deducted from product2
        var product2 = await productRepository.GetProductById(product2Id);
        Assert.NotNull(product2);
        Assert.Equal(5, product2.Quantity); // Should still be 5, not less

        // Cleanup
        await productRepository.DeleteProduct(product1Id);
        await productRepository.DeleteProduct(product2Id);
    }

    [Fact]
    public async Task CreateSale_Should_Rollback_All_Stock_Deductions_On_Failure()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var saleRepository = CreateSaleRepository(connectionString);
        var productRepository = CreateProductRepository(connectionString);

        // Setup: Create three products
        var product1Id = await productRepository.AddProduct(new ProductCreateDTO
        {
            Name = "Product 1",
            SKU = "R1-001",
            Category = "Testing",
            Price = 100m,
            Quantity = 100
        });

        var product2Id = await productRepository.AddProduct(new ProductCreateDTO
        {
            Name = "Product 2",
            SKU = "R2-001",
            Category = "Testing",
            Price = 100m,
            Quantity = 100
        });

        var product3Id = await productRepository.AddProduct(new ProductCreateDTO
        {
            Name = "Product 3 - Insufficient Stock",
            SKU = "R3-001",
            Category = "Testing",
            Price = 100m,
            Quantity = 5
        });

        var saleDto = new SaleCreateDTO
        {
            Items = new List<SaleItemCreateDTO>
            {
                new SaleItemCreateDTO { ProductId = product1Id, Quantity = 25 },
                new SaleItemCreateDTO { ProductId = product2Id, Quantity = 30 },
                new SaleItemCreateDTO { ProductId = product3Id, Quantity = 10 } // Will fail
            }
        };

        // Execute: Try to create sale
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            saleRepository.CreateSaleAsync(saleDto, "cashier1"));

        // Verify: Stock should be completely unchanged for all products
        var p1 = await productRepository.GetProductById(product1Id);
        var p2 = await productRepository.GetProductById(product2Id);
        var p3 = await productRepository.GetProductById(product3Id);

        Assert.Equal(100, p1!.Quantity);
        Assert.Equal(100, p2!.Quantity);
        Assert.Equal(5, p3!.Quantity);

        // Cleanup
        await productRepository.DeleteProduct(product1Id);
        await productRepository.DeleteProduct(product2Id);
        await productRepository.DeleteProduct(product3Id);
    }

    [Fact]
    public async Task CreateSale_Should_Succeed_When_Stock_Is_Exactly_Sufficient()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var saleRepository = CreateSaleRepository(connectionString);
        var productRepository = CreateProductRepository(connectionString);

        // Setup
        var productId = await productRepository.AddProduct(new ProductCreateDTO
        {
            Name = "Exact Stock Product",
            SKU = "EXC-001",
            Category = "Testing",
            Price = 100m,
            Quantity = 50
        });

        var saleDto = new SaleCreateDTO
        {
            Items = new List<SaleItemCreateDTO>
            {
                new SaleItemCreateDTO { ProductId = productId, Quantity = 50 } // Exactly matches stock
            }
        };

        // Execute
        var saleResult = await saleRepository.CreateSaleAsync(saleDto, "cashier1");

        // Verify: Sale created successfully
        Assert.NotNull(saleResult);
        Assert.True(saleResult.SaleId > 0);

        // Verify: Stock is now zero
        var product = await productRepository.GetProductById(productId);
        Assert.NotNull(product);
        Assert.Equal(0, product.Quantity);

        // Cleanup
        await productRepository.DeleteProduct(productId);
    }

    [Fact]
    public async Task CreateSale_Should_Preserve_Transaction_On_Multiple_Items()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var saleRepository = CreateSaleRepository(connectionString);
        var productRepository = CreateProductRepository(connectionString);

        // Setup: Create multiple products
        var product1Id = await productRepository.AddProduct(new ProductCreateDTO
        {
            Name = "Multi Product 1",
            SKU = "M1-001",
            Category = "Testing",
            Price = 500m,
            Quantity = 20
        });

        var product2Id = await productRepository.AddProduct(new ProductCreateDTO
        {
            Name = "Multi Product 2",
            SKU = "M2-001",
            Category = "Testing",
            Price = 750m,
            Quantity = 15
        });

        var product3Id = await productRepository.AddProduct(new ProductCreateDTO
        {
            Name = "Multi Product 3",
            SKU = "M3-001",
            Category = "Testing",
            Price = 1200m,
            Quantity = 10
        });

        var saleDto = new SaleCreateDTO
        {
            Items = new List<SaleItemCreateDTO>
            {
                new SaleItemCreateDTO { ProductId = product1Id, Quantity = 5 },
                new SaleItemCreateDTO { ProductId = product2Id, Quantity = 3 },
                new SaleItemCreateDTO { ProductId = product3Id, Quantity = 2 }
            }
        };

        // Execute
        var saleResult = await saleRepository.CreateSaleAsync(saleDto, "cashier1");

        // Verify: Sale created with correct total
        Assert.NotNull(saleResult);
        var expectedTotal = (500 * 5) + (750 * 3) + (1200 * 2); // 5000 + 2250 + 2400 = 9650
        Assert.Equal(expectedTotal, saleResult.TotalAmount);

        // Verify: Stock deducted correctly
        var p1 = await productRepository.GetProductById(product1Id);
        var p2 = await productRepository.GetProductById(product2Id);
        var p3 = await productRepository.GetProductById(product3Id);

        Assert.Equal(15, p1!.Quantity); // 20 - 5
        Assert.Equal(12, p2!.Quantity); // 15 - 3
        Assert.Equal(8, p3!.Quantity);  // 10 - 2

        // Cleanup
        await productRepository.DeleteProduct(product1Id);
        await productRepository.DeleteProduct(product2Id);
        await productRepository.DeleteProduct(product3Id);
    }
}
