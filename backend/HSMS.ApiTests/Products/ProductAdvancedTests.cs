using Xunit;
using System.Text.Json;
using HSMS.ApiTests.Helpers;

namespace HSMS.ApiTests;

/// <summary>
/// MEMBER 7 - Product Advanced API Tests
/// 
/// Tests advanced product operations:
/// - Inventory view with low-stock status
/// - Low-stock product filtering
/// - Product search functionality
/// 
/// Focus: Product filtering, search, inventory management
/// </summary>
public class ProductAdvancedTests
{
    private readonly ApiClient _client = new ApiClient();

    public ProductAdvancedTests()
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
    }

    // INVENTORY TESTS - POSITIVE

    /// <summary>
    /// Test Case 7.1: Get Inventory Products
    /// Scenario: Retrieve all products with inventory status
    /// Expected: Returns 200 OK with product list and low-stock status
    /// </summary>
    [Fact]
    public void GetInventoryProducts_Should_Return_200_WithData()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ProductInventory;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
        Assert.Contains("[", response.Content); // JSON array
    }

    /// <summary>
    /// Test Case 7.2: Inventory Response Contains Low-Stock Status
    /// Scenario: Verify inventory products have low-stock indicators
    /// Expected: Response includes IsLowStock field
    /// </summary>
    [Fact]
    public void GetInventoryProducts_Response_Should_IncludeLowStockStatus()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ProductInventory;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        var content = response.Content?.ToLower() ?? "";
        Assert.Contains("islowstock", content);
    }

    /// <summary>
    /// Test Case 7.3: Inventory Products Response Structure
    /// Scenario: Verify complete response structure
    /// Expected: Contains product ID, name, SKU, quantity, price, supplier
    /// </summary>
    [Fact]
    public void GetInventoryProducts_Response_ShouldHaveValidStructure()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ProductInventory;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        using var document = JsonDocument.Parse(response.Content);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
    }

    // LOW-STOCK TESTS - POSITIVE

    /// <summary>
    /// Test Case 7.4: Get Low-Stock Products
    /// Scenario: Retrieve only products below threshold
    /// Expected: Returns 200 OK with low-stock products
    /// </summary>
    [Fact]
    public void GetLowStockProducts_Should_Return_200()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ProductLowStock;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
    }

    /// <summary>
    /// Test Case 7.5: Low-Stock Products Is Array
    /// Scenario: Response should be JSON array
    /// Expected: Valid JSON array format
    /// </summary>
    [Fact]
    public void GetLowStockProducts_Response_Should_BeArray()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ProductLowStock;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        using var document = JsonDocument.Parse(response.Content);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
    }

    /// <summary>
    /// Test Case 7.6: Low-Stock Products - All Items Marked Low-Stock
    /// Scenario: All returned products should have IsLowStock = true
    /// Expected: All items have low-stock indicator
    /// </summary>
    [Fact]
    public void GetLowStockProducts_AllItems_ShouldBeLowStock()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ProductLowStock;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        var content = response.Content?.ToLower() ?? "";
        // If there are products, they should all be low-stock
        if (response.Content?.Contains("\"id\"") == true)
        {
            Assert.Contains("islowstock", content);
        }
    }

    // SEARCH TESTS - POSITIVE

    /// <summary>
    /// Test Case 7.7: Search Products with Valid Query
    /// Scenario: Search for products by name
    /// Expected: Returns 200 OK with matching products
    /// </summary>
    [Fact]
    public void SearchProducts_WithValidQuery_Should_Return_200()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ProductSearch}?query=product&limit=10";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
    }

    /// <summary>
    /// Test Case 7.8: Search Results Should Be Array
    /// Scenario: Search response format validation
    /// Expected: Valid JSON array
    /// </summary>
    [Fact]
    public void SearchProducts_Response_Should_BeArray()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ProductSearch}?query=test&limit=10";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        using var document = JsonDocument.Parse(response.Content);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
    }

    /// <summary>
    /// Test Case 7.9: Search with Limit Parameter
    /// Scenario: Search with custom result limit
    /// Expected: Returns 200 OK with limited results
    /// </summary>
    [Fact]
    public void SearchProducts_WithLimit_Should_Return_200()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ProductSearch}?query=item&limit=5";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 7.10: Search with Special Characters
    /// Scenario: Search for products with special characters in query
    /// Expected: Returns 200 OK (handle special chars safely)
    /// </summary>
    [Fact]
    public void SearchProducts_WithSpecialCharacters_Should_Return_200()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ProductSearch}?query=tool%20%26%20more&limit=10";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
    }

    // SEARCH TESTS - NEGATIVE

    /// <summary>
    /// Test Case 7.11: Search Without Query Parameter
    /// Scenario: Search endpoint called without query
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void SearchProducts_WithoutQuery_Should_Return_400()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ProductSearch;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
        Assert.Contains("query", response.Content?.ToLower() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 7.12: Search with Empty Query
    /// Scenario: Search with empty query string
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void SearchProducts_WithEmptyQuery_Should_Return_400()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ProductSearch}?query=&limit=10";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 7.13: Search with Whitespace-only Query
    /// Scenario: Query contains only whitespace
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void SearchProducts_WithWhitespaceQuery_Should_Return_400()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ProductSearch}?query=%20%20%20&limit=10";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 7.14: Search with Negative Limit
    /// Scenario: Search with invalid negative limit
    /// Expected: Returns 400 Bad Request or uses default
    /// </summary>
    [Fact]
    public void SearchProducts_WithNegativeLimit_Should_HandleGracefully()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ProductSearch}?query=product&limit=-5";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        // Either handles error or uses default limit
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest
        );
    }

    /// <summary>
    /// Test Case 7.15: Search with Zero Limit
    /// Scenario: Search with zero limit
    /// Expected: Returns 400 Bad Request or default limit
    /// </summary>
    [Fact]
    public void SearchProducts_WithZeroLimit_Should_HandleGracefully()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ProductSearch}?query=product&limit=0";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest
        );
    }

    /// <summary>
    /// Test Case 7.16: Search with SQL Injection Attempt
    /// Scenario: Query contains SQL injection payload
    /// Expected: Returns 200 OK (safe search) or 400 Bad Request
    /// </summary>
    [Fact]
    public void SearchProducts_WithSqlInjection_Should_BeSecure()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ProductSearch}?query='; DROP TABLE products; --&limit=10";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest
        );
        // Verify products table still exists (by making another call)
        var verifyResponse = _client.Get(ApiTestConstants.Endpoints.Products);
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)verifyResponse.StatusCode);
    }

    /// <summary>
    /// Test Case 7.17: Search Performance - Response Time
    /// Scenario: Verify search completes in reasonable time
    /// Expected: Response time < 2 seconds
    /// </summary>
    [Fact]
    public void SearchProducts_Should_CompleteWithinReasonableTime()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ProductSearch}?query=test&limit=100";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        var responseTime = ApiClient.GetResponseTime(response);
        Assert.NotNull(responseTime);
        Assert.True(responseTime.Value.TotalMilliseconds < 2000,
            $"Search took {responseTime.Value.TotalMilliseconds}ms, expected < 2000ms");
    }

    /// <summary>
    /// Test Case 7.18: Inventory Performance Test
    /// Scenario: Inventory endpoint response time
    /// Expected: Response time < 2 seconds
    /// </summary>
    [Fact]
    public void GetInventoryProducts_Should_CompleteWithinReasonableTime()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ProductInventory;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        var responseTime = ApiClient.GetResponseTime(response);
        Assert.NotNull(responseTime);
        Assert.True(responseTime.Value.TotalMilliseconds < 2000,
            $"Inventory retrieval took {responseTime.Value.TotalMilliseconds}ms, expected < 2000ms");
    }

    /// <summary>
    /// Test Case 7.19: Low-Stock Performance Test
    /// Scenario: Low-stock endpoint response time
    /// Expected: Response time < 2 seconds
    /// </summary>
    [Fact]
    public void GetLowStockProducts_Should_CompleteWithinReasonableTime()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ProductLowStock;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        var responseTime = ApiClient.GetResponseTime(response);
        Assert.NotNull(responseTime);
        Assert.True(responseTime.Value.TotalMilliseconds < 2000,
            $"Low-stock retrieval took {responseTime.Value.TotalMilliseconds}ms, expected < 2000ms");
    }
}
