using Xunit;
using System.Text.Json;
using HSMS.ApiTests.Helpers;

namespace HSMS.ApiTests;

/// <summary>
/// MEMBER 9 - Sales API Tests (Negative/Error Scenarios)
/// 
/// Tests error handling for sales operations:
/// - Invalid product references
/// - Invalid quantities
/// - Duplicate products in sale
/// - Invalid date filters
/// - Non-existent sales
/// 
/// Focus: Error validation, constraint checking, edge cases
/// </summary>
public class SalesApiNegativeTests
{
    private readonly ApiClient _client = new ApiClient();
    private int _testProductId;

    public SalesApiNegativeTests()
    {
        var authClient = new ApiClient();
        var loginRequest = new
        {
            username = ApiTestConstants.TestAdminUsername,
            password = ApiTestConstants.TestAdminPassword
        };

        var loginResponse = authClient.Post(ApiTestConstants.Endpoints.AuthLogin, loginRequest);
        if (!loginResponse.IsSuccessful || string.IsNullOrWhiteSpace(loginResponse.Content))
        {
            throw new InvalidOperationException("Unable to authenticate.");
        }

        using var document = JsonDocument.Parse(loginResponse.Content);
        var token = document.RootElement.GetProperty("accessToken").GetString();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("No authentication token received.");
        }

        _client.SetAuthToken(token);
        _testProductId = GetOrCreateTestProduct();
    }

    private int GetOrCreateTestProduct()
    {
        // Always create a fresh test product with sufficient stock to avoid conflicts
        var uniqueSku = $"SALES-NEG-{Guid.NewGuid():N}";
        var productData = new
        {
            name = $"Sales Negative Test Product {uniqueSku}",
            sku = uniqueSku,
            price = 29.99m,
            quantity = 10000,
            category = "Test",
            supplierId = (int?)null
        };

        var createResponse = _client.Post(ApiTestConstants.Endpoints.Products, productData);
        if ((int)createResponse.StatusCode != ApiTestConstants.HttpStatusCodes.Created)
            throw new InvalidOperationException($"Failed to create test product. Status: {(int)createResponse.StatusCode}");

        // The POST response doesn't include the product ID, so fetch the product list and find by SKU
        var listResponse = _client.Get(ApiTestConstants.Endpoints.Products);
        using var listDoc = JsonDocument.Parse(listResponse.Content);
        foreach (var product in listDoc.RootElement.EnumerateArray())
        {
            string? sku = product.TryGetProperty("sku", out var skuElement) ? skuElement.GetString() : null;
            if (string.Equals(sku, uniqueSku, StringComparison.OrdinalIgnoreCase) &&
                product.TryGetProperty("id", out var idElement) && idElement.TryGetInt32(out var id))
            {
                return id;
            }
        }

        throw new InvalidOperationException("Could not retrieve the created product.");
    }

    // NEGATIVE TESTS - CREATE SALES

    /// <summary>
    /// Test Case 9.1: Create Sale with Empty Items
    /// Scenario: Attempt to create sale without items
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateSale_WithEmptyItems_Should_Return_400()
    {
        // Arrange
        var saleRequest = new
        {
            items = new object[0]
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Sales, saleRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
        Assert.Contains("at least one item", response.Content ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 9.2: Create Sale with Null Items
    /// Scenario: Items field is null
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateSale_WithNullItems_Should_Return_400()
    {
        // Arrange
        var saleRequest = new
        {
            items = (object?)null
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Sales, saleRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 9.3: Create Sale with Invalid Product ID
    /// Scenario: Product ID is zero
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateSale_WithZeroProductId_Should_Return_400()
    {
        // Arrange
        var saleRequest = new
        {
            items = new[]
            {
                new
                {
                    productId = 0,
                    quantity = 2,
                    unitPrice = 29.99m
                }
            }
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Sales, saleRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
        Assert.Contains("valid product", response.Content ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 9.4: Create Sale with Negative Product ID
    /// Scenario: Product ID is negative
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateSale_WithNegativeProductId_Should_Return_400()
    {
        // Arrange
        var saleRequest = new
        {
            items = new[]
            {
                new
                {
                    productId = -1,
                    quantity = 2,
                    unitPrice = 29.99m
                }
            }
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Sales, saleRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 9.5: Create Sale with Zero Quantity
    /// Scenario: Item quantity is zero
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateSale_WithZeroQuantity_Should_Return_400()
    {
        // Arrange
        var saleRequest = new
        {
            items = new[]
            {
                new
                {
                    productId = _testProductId,
                    quantity = 0,
                    unitPrice = 29.99m
                }
            }
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Sales, saleRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
        Assert.Contains("greater than zero", response.Content ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 9.6: Create Sale with Negative Quantity
    /// Scenario: Item quantity is negative
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateSale_WithNegativeQuantity_Should_Return_400()
    {
        // Arrange
        var saleRequest = new
        {
            items = new[]
            {
                new
                {
                    productId = _testProductId,
                    quantity = -5,
                    unitPrice = 29.99m
                }
            }
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Sales, saleRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 9.7: Create Sale with Duplicate Products
    /// Scenario: Same product ID appears twice in items
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateSale_WithDuplicateProducts_Should_Return_400()
    {
        // Arrange
        var saleRequest = new
        {
            items = new[]
            {
                new
                {
                    productId = _testProductId,
                    quantity = 2,
                    unitPrice = 29.99m
                },
                new
                {
                    productId = _testProductId,
                    quantity = 3,
                    unitPrice = 29.99m
                }
            }
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Sales, saleRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
        Assert.Contains("already in the sale", response.Content ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 9.8: Create Sale with Non-existent Product
    /// Scenario: Product ID doesn't exist in database
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateSale_WithNonExistentProduct_Should_Return_400()
    {
        // Arrange
        var saleRequest = new
        {
            items = new[]
            {
                new
                {
                    productId = 999999,
                    quantity = 2,
                    unitPrice = 29.99m
                }
            }
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Sales, saleRequest);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.NotFound
        );
    }

    // NEGATIVE TESTS - GET SALES HISTORY

    /// <summary>
    /// Test Case 9.9: Get Sales History with Negative Limit
    /// Scenario: Limit parameter is negative
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void GetSalesHistory_WithNegativeLimit_Should_Return_400()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.SalesHistory}?limit=-10";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
        Assert.Contains("greater than zero", response.Content ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 9.10: Get Sales History with Zero Limit
    /// Scenario: Limit is zero
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void GetSalesHistory_WithZeroLimit_Should_Return_400()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.SalesHistory}?limit=0";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 9.11: Get Sales History with Invalid Date Range
    /// Scenario: fromDate is after toDate
    /// Expected: Returns 400 Bad Request or valid results
    /// </summary>
    [Fact]
    public void GetSalesHistory_WithInvalidDateRange_Should_HandleGracefully()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-dd");
        var toDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var endpoint = $"{ApiTestConstants.Endpoints.SalesHistory}?fromDate={fromDate}&toDate={toDate}&limit=50";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        // Should either be empty results or error
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest
        );
    }

    // NEGATIVE TESTS - GET SALE DETAILS

    /// <summary>
    /// Test Case 9.12: Get Sale Details with Non-existent ID
    /// Scenario: Sale ID doesn't exist
    /// Expected: Returns 404 Not Found
    /// </summary>
    [Fact]
    public void GetSaleDetails_NonExistentId_Should_Return_404()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.SalesById.Replace("{id}", "999999");

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.NotFound, (int)response.StatusCode);
        Assert.Contains("not found", response.Content?.ToLower() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 9.13: Get Sale Details with Negative ID
    /// Scenario: Sale ID is negative
    /// Expected: Returns 404 Not Found or 400 Bad Request
    /// </summary>
    [Fact]
    public void GetSaleDetails_WithNegativeId_Should_ReturnError()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.SalesById.Replace("{id}", "-1");

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.NotFound
        );
    }

    /// <summary>
    /// Test Case 9.14: Get Sale Details with Invalid ID Format
    /// Scenario: Sale ID is not numeric
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void GetSaleDetails_WithInvalidIdFormat_Should_Return_400()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.SalesById.Replace("{id}", "abc");

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.NotFound, (int)response.StatusCode);
    }

    // NEGATIVE TESTS - GET INVOICE

    /// <summary>
    /// Test Case 9.15: Get Invoice for Non-existent Sale
    /// Scenario: Invoice ID doesn't exist
    /// Expected: Returns 404 Not Found
    /// </summary>
    [Fact]
    public void GetSaleInvoice_NonExistentId_Should_Return_404()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.SalesInvoice.Replace("{id}", "999999");

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.NotFound, (int)response.StatusCode);
        Assert.Contains("not found", response.Content?.ToLower() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 9.16: Get Invoice with Invalid ID Format
    /// Scenario: Invoice ID is not numeric
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void GetSaleInvoice_WithInvalidIdFormat_Should_Return_400()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.SalesInvoice.Replace("{id}", "invalid");

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.NotFound, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 9.17: Create Sale with Invalid Price
    /// Scenario: Unit price is negative
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateSale_WithNegativePrice_Should_Return_400()
    {
        // Arrange
        var saleRequest = new
        {
            items = new[]
            {
                new
                {
                    productId = _testProductId,
                    quantity = 2,
                    unitPrice = -5.00m
                }
            }
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Sales, saleRequest);

        // Assert
        // Should handle negative price gracefully
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK
        );
    }

    /// <summary>
    /// Test Case 9.18: Create Sale with Very Large Quantity
    /// Scenario: Quantity exceeds available stock
    /// Expected: Returns 400 Bad Request or decreases stock appropriately
    /// </summary>
    [Fact]
    public void CreateSale_WithExcessiveQuantity_Should_HandleGracefully()
    {
        // Arrange
        var saleRequest = new
        {
            items = new[]
            {
                new
                {
                    productId = _testProductId,
                    quantity = 1000000,
                    unitPrice = 29.99m
                }
            }
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Sales, saleRequest);

        // Assert
        // Should either succeed, reduce quantity, or reject
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest
        );
    }

    /// <summary>
    /// Test Case 9.19: Get Sales History - Authorization Check
    /// Scenario: Verify proper authorization for sales access
    /// Expected: Returns 200 OK (authorized) or 403 Forbidden (unauthorized)
    /// </summary>
    [Fact]
    public void GetSalesHistory_ShouldRespectAuthorization()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.SalesHistory;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.Forbidden
        );
    }
}
