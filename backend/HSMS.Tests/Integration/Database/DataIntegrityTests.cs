using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using HSMS.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace HSMS.Tests;

/// <summary>
/// Tests for data integrity and orphan handling
/// Verifies referential integrity and cascading behavior
/// Requires: HSMS_TEST_CONNECTION_STRING environment variable
/// </summary>
[Collection("DatabaseIntegration")]
public class DataIntegrityTests
{
    private static string? GetConnectionString() => System.Environment.GetEnvironmentVariable("HSMS_TEST_CONNECTION_STRING");

    private static ProductRepository CreateProductRepository(string connectionString)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", connectionString) })
            .Build();
        return new ProductRepository(config);
    }

    private static SupplierRepository CreateSupplierRepository(string connectionString)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", connectionString) })
            .Build();
        return new SupplierRepository(config);
    }

    private static SaleRepository CreateSaleRepository(string connectionString)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", connectionString) })
            .Build();
        return new SaleRepository(config);
    }

    private static UserRepository CreateUserRepository(string connectionString)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", connectionString) })
            .Build();
        return new UserRepository(config);
    }

    private static async Task DeleteSaleAsync(string connectionString, int saleId)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand("DELETE FROM Sales WHERE Id = @Id", connection);
        command.Parameters.AddWithValue("@Id", saleId);
        await command.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Supplier_Should_Not_Be_Deletable_When_Products_Are_Linked()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var supplierRepo = CreateSupplierRepository(connectionString);
        var productRepo = CreateProductRepository(connectionString);

        // Setup: Create supplier and product linked to it
        var supplierId = await supplierRepo.AddSupplierAsync(new SupplierCreateDTO
        {
            Name = $"Linked Supplier {System.Guid.NewGuid()}",
            ContactInfo = "supplier@test.com"
        });

        var productId = await productRepo.AddProduct(new ProductCreateDTO
        {
            Name = "Product Linked to Supplier",
            SKU = "LINK-001",
            Category = "Testing",
            Price = 100m,
            Quantity = 10,
            SupplierId = supplierId
        });

        // Execute: Try to delete supplier
        var deleteStatus = await supplierRepo.DeleteSupplierAsync(supplierId);

        // Verify: Deletion should fail due to linked products
        Assert.Equal(SupplierDeleteStatus.LinkedRecordsExist, deleteStatus);

        // Verify: Supplier should still exist
        var suppliers = await supplierRepo.GetSuppliersAsync();
        Assert.Contains(suppliers, s => s.Id == supplierId);

        // Cleanup
        await productRepo.DeleteProduct(productId);
        await supplierRepo.DeleteSupplierAsync(supplierId);
    }

    [Fact]
    public async Task Supplier_Should_Be_Deletable_When_No_Products_Linked()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var supplierRepo = CreateSupplierRepository(connectionString);

        // Setup: Create supplier without linked products
        var supplierId = await supplierRepo.AddSupplierAsync(new SupplierCreateDTO
        {
            Name = $"Unlinked Supplier {System.Guid.NewGuid()}",
            ContactInfo = "unlinked@test.com"
        });

        // Execute: Delete supplier
        var deleteStatus = await supplierRepo.DeleteSupplierAsync(supplierId);

        // Verify: Deletion should succeed
        Assert.Equal(SupplierDeleteStatus.Deleted, deleteStatus);

        // Verify: Supplier should no longer exist
        var suppliers = await supplierRepo.GetSuppliersAsync();
        Assert.DoesNotContain(suppliers, s => s.Id == supplierId);
    }

    [Fact]
    public async Task Product_Deletion_Should_Not_Affect_Completed_Sales()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var productRepo = CreateProductRepository(connectionString);
        var saleRepo = CreateSaleRepository(connectionString);
        var userRepo = CreateUserRepository(connectionString);

        // Setup: Create user, product, and sale
        var username = $"testuser_{System.Guid.NewGuid():N}";
        var userId = await userRepo.CreateUserAsync(username, "hashed_password", "Cashier");

        var productId = await productRepo.AddProduct(new ProductCreateDTO
        {
            Name = "Product for Sale",
            SKU = "SALE-001",
            Category = "Testing",
            Price = 500m,
            Quantity = 100
        });

        var saleDto = new SaleCreateDTO
        {
            Items = new List<SaleItemCreateDTO>
            {
                new SaleItemCreateDTO { ProductId = productId, Quantity = 10 }
            }
        };

        var user = await userRepo.GetByIdAsync(userId);
        var sale = await saleRepo.CreateSaleAsync(saleDto, user!.Username);

        // Execute: Try to delete the product while a sale item still references it
        var deleteException = await Assert.ThrowsAsync<MySqlException>(() => productRepo.DeleteProduct(productId));
        Assert.Contains("foreign key", deleteException.Message, System.StringComparison.OrdinalIgnoreCase);

        // Verify: Sale record still exists
        var saleHistory = await saleRepo.GetSalesHistoryAsync(sale.SaleId, null, null);
        Assert.NotEmpty(saleHistory);
        Assert.Single(saleHistory);
        Assert.Equal(username, saleHistory[0].SoldBy);
        Assert.Equal(1, saleHistory[0].ItemCount);

        // Cleanup
        await DeleteSaleAsync(connectionString, sale.SaleId);
        await productRepo.DeleteProduct(productId);
        await userRepo.DeleteAsync(userId);
    }

    [Fact]
    public async Task User_Deletion_Should_Preserve_Sale_Records()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var userRepo = CreateUserRepository(connectionString);
        var productRepo = CreateProductRepository(connectionString);
        var saleRepo = CreateSaleRepository(connectionString);

        // Setup: Create user and make a sale
        var userId = await userRepo.CreateUserAsync($"cashier_{System.Guid.NewGuid()}", "hashed_pass", "Cashier");
        var user = await userRepo.GetByIdAsync(userId);

        var productId = await productRepo.AddProduct(new ProductCreateDTO
        {
            Name = "Product by Deleted User",
            SKU = "USR-001",
            Category = "Testing",
            Price = 200m,
            Quantity = 50
        });

        var saleDto = new SaleCreateDTO
        {
            Items = new List<SaleItemCreateDTO>
            {
                new SaleItemCreateDTO { ProductId = productId, Quantity = 5 }
            }
        };

        var sale = await saleRepo.CreateSaleAsync(saleDto, user!.Username);

        // Execute: Delete the user
        var userDeleteResult = await userRepo.DeleteAsync(userId);
        Assert.True(userDeleteResult);

        // Verify: Sale record still exists with username snapshot
        var saleHistory = await saleRepo.GetSalesHistoryAsync(sale.SaleId, null, null);
        Assert.NotEmpty(saleHistory);
        Assert.Equal(user.Username, saleHistory[0].SoldBy);

        // Cleanup
        await DeleteSaleAsync(connectionString, sale.SaleId);
        await productRepo.DeleteProduct(productId);
    }

    [Fact]
    public async Task Product_Should_Store_Price_And_Name_Snapshot_In_Sale_Item()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var productRepo = CreateProductRepository(connectionString);
        var saleRepo = CreateSaleRepository(connectionString);
        var userRepo = CreateUserRepository(connectionString);

        // Setup
        var userId = await userRepo.CreateUserAsync($"cashier_snap_{System.Guid.NewGuid():N}", "hashed_pass", "Cashier");
        var user = await userRepo.GetByIdAsync(userId);

        var productId = await productRepo.AddProduct(new ProductCreateDTO
        {
            Name = "Original Product Name",
            SKU = "SNAP-001",
            Category = "Testing",
            Price = 1000m,
            Quantity = 20
        });

        // Make sale
        var saleDto = new SaleCreateDTO
        {
            Items = new List<SaleItemCreateDTO>
            {
                new SaleItemCreateDTO { ProductId = productId, Quantity = 3 }
            }
        };

        var sale = await saleRepo.CreateSaleAsync(saleDto, user!.Username);

        // Update product after sale
        await productRepo.UpdateProduct(productId, new ProductUpdateDTO
        {
            Name = "Updated Product Name",
            SKU = "SNAP-001",
            Category = "Testing",
            Price = 2000m,
            Quantity = 20,
            SupplierId = null
        });

        // Verify: Sale item should have original snapshot
        var saleDetails = await saleRepo.GetSaleDetailsAsync(sale.SaleId);
        Assert.NotNull(saleDetails);
        var saleItem = Assert.Single(saleDetails.Items);

        Assert.Equal("Original Product Name", saleItem.ProductName);
        Assert.Equal(1000m, saleItem.UnitPrice);
        Assert.Equal(3000m, saleItem.LineSubtotal); // 1000 * 3

        // Cleanup
        await DeleteSaleAsync(connectionString, sale.SaleId);
        await productRepo.DeleteProduct(productId);
        await userRepo.DeleteAsync(userId);
    }

    [Fact]
    public async Task Sale_Items_Should_Maintain_Correct_Quantities()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var productRepo = CreateProductRepository(connectionString);
        var saleRepo = CreateSaleRepository(connectionString);
        var userRepo = CreateUserRepository(connectionString);

        // Setup
        var userId = await userRepo.CreateUserAsync($"qty_cashier_{System.Guid.NewGuid():N}", "hashed_pass", "Cashier");
        var user = await userRepo.GetByIdAsync(userId);

        var product1 = await productRepo.AddProduct(new ProductCreateDTO
        {
            Name = "Product 1",
            SKU = "QTY1",
            Category = "Testing",
            Price = 100m,
            Quantity = 100
        });

        var product2 = await productRepo.AddProduct(new ProductCreateDTO
        {
            Name = "Product 2",
            SKU = "QTY2",
            Category = "Testing",
            Price = 200m,
            Quantity = 100
        });

        // Create sale with multiple items
        var saleDto = new SaleCreateDTO
        {
            Items = new List<SaleItemCreateDTO>
            {
                new SaleItemCreateDTO { ProductId = product1, Quantity = 7 },
                new SaleItemCreateDTO { ProductId = product2, Quantity = 3 },
                new SaleItemCreateDTO { ProductId = product1, Quantity = 2 }  // Same product, second line
            }
        };

        var sale = await saleRepo.CreateSaleAsync(saleDto, user!.Username);

        // Verify: Sale items maintain correct quantities
        var saleDetails = await saleRepo.GetSaleDetailsAsync(sale.SaleId);
        Assert.NotNull(saleDetails);
        Assert.Equal(3, saleDetails.Items.Count);

        var item1 = saleDetails.Items[0];
        var item2 = saleDetails.Items[1];
        var item3 = saleDetails.Items[2];

        Assert.Equal(7, item1.Quantity);
        Assert.Equal(3, item2.Quantity);
        Assert.Equal(2, item3.Quantity);

        // Verify: Product stock updated correctly
        var p1 = await productRepo.GetProductById(product1);
        var p2 = await productRepo.GetProductById(product2);

        Assert.Equal(91, p1!.Quantity); // 100 - 7 - 2
        Assert.Equal(97, p2!.Quantity); // 100 - 3

        // Cleanup
        await DeleteSaleAsync(connectionString, sale.SaleId);
        await productRepo.DeleteProduct(product1);
        await productRepo.DeleteProduct(product2);
        await userRepo.DeleteAsync(userId);
    }

    [Fact]
    public async Task Sale_Total_Should_Be_Calculated_Correctly()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var productRepo = CreateProductRepository(connectionString);
        var saleRepo = CreateSaleRepository(connectionString);
        var userRepo = CreateUserRepository(connectionString);

        // Setup
        var userId = await userRepo.CreateUserAsync($"total_cashier_{System.Guid.NewGuid():N}", "hashed_pass", "Cashier");
        var user = await userRepo.GetByIdAsync(userId);

        var product1 = await productRepo.AddProduct(new ProductCreateDTO
        {
            Name = "Item A",
            SKU = "ITMA",
            Category = "Testing",
            Price = 1500.50m,
            Quantity = 100
        });

        var product2 = await productRepo.AddProduct(new ProductCreateDTO
        {
            Name = "Item B",
            SKU = "ITMB",
            Category = "Testing",
            Price = 2250.75m,
            Quantity = 100
        });

        var saleDto = new SaleCreateDTO
        {
            Items = new List<SaleItemCreateDTO>
            {
                new SaleItemCreateDTO { ProductId = product1, Quantity = 2 },  // 1500.50 * 2 = 3001.00
                new SaleItemCreateDTO { ProductId = product2, Quantity = 3 }   // 2250.75 * 3 = 6752.25
            }
        };

        var sale = await saleRepo.CreateSaleAsync(saleDto, user!.Username);

        // Verify: Total = 3001.00 + 6752.25 = 9753.25
        Assert.Equal(9753.25m, sale.TotalAmount);

        // Cleanup
        await DeleteSaleAsync(connectionString, sale.SaleId);
        await productRepo.DeleteProduct(product1);
        await productRepo.DeleteProduct(product2);
        await userRepo.DeleteAsync(userId);
    }
}
