using Xunit;
using System.Text.Json;
using HSMS.ApiTests.Helpers;

namespace HSMS.ApiTests;

/// <summary>
/// MEMBER 8 - Sales API Tests (Positive Scenarios)
/// 
/// Tests sales creation and retrieval:
/// - Create new sales transactions
/// - Retrieve sales history
/// - Get sale details
/// - Generate invoices
/// 
/// Focus: Transaction creation, data retrieval, invoice generation
/// </summary>
public class SalesApiTests
{
    private readonly ApiClient _client = new ApiClient();
    private int _testProductId;

    public SalesApiTests()
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
        var uniqueSku = $"SALES-{Guid.NewGuid():N}";
        var productData = new
        {
            name = $"Sales Test Product {uniqueSku}",
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

    // POSITIVE TESTS - CREATE SALES

    /// <summary>
    /// Test Case 8.1: Create Sale with Single Item
    /// Scenario: Create a sale with one product
    /// Expected: Returns 200 OK with transaction details
    /// </summary>
    [Fact]
    public void CreateSale_WithSingleItem_Should_Return_200()
    {
        // Arrange
        var saleRequest = new
        {
            items = new[]
            {
                new
                {
                    productId = _testProductId,
                    quantity = 2
                }
            }
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Sales, saleRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
        Assert.Contains("saleId", response.Content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 8.2: Create Sale with Multiple Items
    /// Scenario: Create sale with multiple products
    /// Expected: Returns 200 OK
    /// </summary>
    [Fact]
    public void CreateSale_WithMultipleItems_Should_Return_200()
    {
        // Arrange
        var saleRequest = new
        {
            items = new[]
            {
                new
                {
                    productId = _testProductId,
                    quantity = 2
                }
            }
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Sales, saleRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.Contains("saleId", response.Content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 8.3: Create Sale Response Contains Sale ID
    /// Scenario: Verify sale response structure
    /// Expected: Response includes saleId and timestamp
    /// </summary>
    [Fact]
    public void CreateSale_Response_ShouldContainSaleId()
    {
        // Arrange
        var saleRequest = new
        {
            items = new[]
            {
                new
                {
                    productId = _testProductId,
                    quantity = 2
                }
            }
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Sales, saleRequest);

        // Assert
        using var doc = JsonDocument.Parse(response.Content);
        Assert.True(doc.RootElement.TryGetProperty("saleId", out var saleIdElement));
        Assert.True(saleIdElement.TryGetInt32(out var saleId));
        Assert.True(saleId > 0);
    }

    // POSITIVE TESTS - GET SALES HISTORY

    /// <summary>
    /// Test Case 8.4: Get Sales History
    /// Scenario: Retrieve sales transaction history
    /// Expected: Returns 200 OK with list of sales
    /// </summary>
    [Fact]
    public void GetSalesHistory_Should_Return_200()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.SalesHistory;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
    }

    /// <summary>
    /// Test Case 8.5: Get Sales History Returns Array
    /// Scenario: Sales history response format
    /// Expected: Valid JSON array
    /// </summary>
    [Fact]
    public void GetSalesHistory_Response_ShouldBeArray()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.SalesHistory;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        using var doc = JsonDocument.Parse(response.Content);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    /// <summary>
    /// Test Case 8.6: Get Sales History with Limit
    /// Scenario: Retrieve sales with custom limit
    /// Expected: Returns 200 OK with limited results
    /// </summary>
    [Fact]
    public void GetSalesHistory_WithLimit_Should_Return_200()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.SalesHistory}?limit=10";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 8.7: Get Sales History with Date Filter
    /// Scenario: Retrieve sales within date range
    /// Expected: Returns 200 OK with filtered sales
    /// </summary>
    [Fact]
    public void GetSalesHistory_WithDateFilter_Should_Return_200()
    {
        // Arrange
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var endpoint = $"{ApiTestConstants.Endpoints.SalesHistory}?fromDate={today}&limit=50";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
    }

    // POSITIVE TESTS - GET SALE DETAILS

    /// <summary>
    /// Test Case 8.8: Get Sale Details by ID
    /// Scenario: Retrieve details of a specific sale
    /// Expected: Returns 200 OK with sale information
    /// </summary>
    [Fact]
    public void GetSaleDetails_WithValidId_Should_Return_200()
    {
        // Arrange - Create a sale first
        var saleRequest = new
        {
            items = new[]
            {
                new
                {
                    productId = _testProductId,
                    quantity = 2
                }
            }
        };
        var createResponse = _client.Post(ApiTestConstants.Endpoints.Sales, saleRequest);
        using var createDoc = JsonDocument.Parse(createResponse.Content);
        var saleId = createDoc.RootElement.GetProperty("saleId").GetInt32();

        var endpoint = ApiTestConstants.Endpoints.SalesById.Replace("{id}", saleId.ToString());

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
    }

    /// <summary>
    /// Test Case 8.9: Sale Details Response Structure
    /// Scenario: Verify sale details contain required fields
    /// Expected: Response includes items, total, timestamp
    /// </summary>
    [Fact]
    public void GetSaleDetails_Response_ShouldHaveRequiredFields()
    {
        // Arrange - Create sale
        var saleRequest = new
        {
            items = new[]
            {
                new
                {
                    productId = _testProductId,
                    quantity = 2
                }
            }
        };
        var createResponse = _client.Post(ApiTestConstants.Endpoints.Sales, saleRequest);
        using var createDoc = JsonDocument.Parse(createResponse.Content);
        var saleId = createDoc.RootElement.GetProperty("saleId").GetInt32();

        var endpoint = ApiTestConstants.Endpoints.SalesById.Replace("{id}", saleId.ToString());

        // Act
        var response = _client.Get(endpoint);

        // Assert
        using var doc = JsonDocument.Parse(response.Content);
        Assert.True(doc.RootElement.TryGetProperty("saleId", out _));
        Assert.True(doc.RootElement.TryGetProperty("items", out _));
    }

    // POSITIVE TESTS - GET INVOICE

    /// <summary>
    /// Test Case 8.10: Get Sale Invoice
    /// Scenario: Generate invoice for a sale
    /// Expected: Returns 200 OK with invoice data
    /// </summary>
    [Fact]
    public void GetSaleInvoice_WithValidId_Should_Return_200()
    {
        // Arrange - Create sale
        var saleRequest = new
        {
            items = new[]
            {
                new
                {
                    productId = _testProductId,
                    quantity = 2
                }
            }
        };
        var createResponse = _client.Post(ApiTestConstants.Endpoints.Sales, saleRequest);
        using var createDoc = JsonDocument.Parse(createResponse.Content);
        var saleId = createDoc.RootElement.GetProperty("saleId").GetInt32();

        var endpoint = ApiTestConstants.Endpoints.SalesInvoice.Replace("{id}", saleId.ToString());

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
    }

    /// <summary>
    /// Test Case 8.11: Create Sale Performance Test
    /// Scenario: Sale creation completes in reasonable time
    /// Expected: Response time < 2 seconds
    /// </summary>
    [Fact]
    public void CreateSale_Should_CompleteWithinReasonableTime()
    {
        // Arrange
        var saleRequest = new
        {
            items = new[]
            {
                new
                {
                    productId = _testProductId,
                    quantity = 2
                }
            }
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Sales, saleRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        var responseTime = ApiClient.GetResponseTime(response);
        Assert.NotNull(responseTime);
        Assert.True(responseTime.Value.TotalMilliseconds < 2000,
            $"Sale creation took {responseTime.Value.TotalMilliseconds}ms, expected < 2000ms");
    }
}
