using Xunit;
using System.Text.Json;
using HSMS.ApiTests.Helpers;

namespace HSMS.ApiTests;

/// <summary>
/// MEMBER 6 - Product Update/Delete API Tests
/// 
/// Tests product update and delete operations:
/// - Update product details
/// - Update product stock
/// - Delete products
/// - Error handling for invalid operations
/// 
/// Focus: Product modification, stock management, data integrity
/// </summary>
public class ProductUpdateDeleteTests
{
    private readonly ApiClient _client = new ApiClient();
    private int _testProductId;

    public ProductUpdateDeleteTests()
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
            throw new InvalidOperationException("Unable to authenticate for product tests.");
        }

        using var document = JsonDocument.Parse(loginResponse.Content);
        var token = document.RootElement.GetProperty("accessToken").GetString();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Authentication response did not contain an access token.");
        }

        _client.SetAuthToken(token);
        _testProductId = CreateTestProduct();
    }

    private int CreateTestProduct()
    {
        var uniqueSku = $"UPD-{Guid.NewGuid():N}";
        var productData = new
        {
            name = $"Update Test Product {uniqueSku}",
            sku = uniqueSku,
            price = 49.99m,
            quantity = 100,
            category = "Test Category",
            supplierId = (int?)null
        };

        var createResponse = _client.Post(ApiTestConstants.Endpoints.Products, productData);
        if ((int)createResponse.StatusCode != ApiTestConstants.HttpStatusCodes.Created)
        {
            throw new InvalidOperationException($"Failed to create test product. Status: {(int)createResponse.StatusCode}");
        }

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

        throw new InvalidOperationException("Failed to retrieve created product.");
    }

    // POSITIVE TESTS - UPDATE

    /// <summary>
    /// Test Case 6.1: Update Product with Valid Data
    /// Scenario: Update product name, price, and quantity
    /// Expected: Returns 200 OK with success message
    /// </summary>
    [Fact]
    public void UpdateProduct_WithValidData_Should_Return_200()
    {
        // Arrange
        var updateRequest = new
        {
            name = "Updated Product Name",
            sku = "UPD-PROD-001",
            price = 59.99m,
            quantity = 150,
            category = "Updated Category",
            supplierId = (int?)null
        };
        var endpoint = ApiTestConstants.Endpoints.ProductById.Replace("{id}", _testProductId.ToString());

        // Act
        var response = _client.Put(endpoint, updateRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.Contains("updated successfully", response.Content ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 6.2: Update Product Price Only
    /// Scenario: Change only the product price
    /// Expected: Returns 200 OK
    /// </summary>
    [Fact]
    public void UpdateProduct_ChangePriceOnly_Should_Return_200()
    {
        // Arrange
        var getResponse = _client.Get(ApiTestConstants.Endpoints.ProductById.Replace("{id}", _testProductId.ToString()));
        using var doc = JsonDocument.Parse(getResponse.Content);
        var name = doc.RootElement.GetProperty("name").GetString();
        var sku = doc.RootElement.GetProperty("sku").GetString();
        var quantity = doc.RootElement.GetProperty("quantity").GetInt32();
        var category = doc.RootElement.GetProperty("category").GetString();

        var updateRequest = new
        {
            name = name,
            sku = sku,
            price = 99.99m,
            quantity = quantity,
            category = category,
            supplierId = (int?)null
        };
        var endpoint = ApiTestConstants.Endpoints.ProductById.Replace("{id}", _testProductId.ToString());

        // Act
        var response = _client.Put(endpoint, updateRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 6.3: Update Product Stock
    /// Scenario: Update stock quantity for a product
    /// Expected: Returns 200 OK
    /// </summary>
    [Fact]
    public void UpdateProductStock_WithValidQuantity_Should_Return_200()
    {
        // Arrange
        var updateRequest = new
        {
            quantity = 200,
            reason = "Stock adjustment - inventory count"
        };
        var endpoint = ApiTestConstants.Endpoints.ProductStock.Replace("{id}", _testProductId.ToString());

        // Act
        var response = _client.Put(endpoint, updateRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.Contains("stock updated successfully", response.Content ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 6.4: Update Product Stock to Zero
    /// Scenario: Set stock quantity to zero
    /// Expected: Returns 200 OK
    /// </summary>
    [Fact]
    public void UpdateProductStock_ToZero_Should_Return_200()
    {
        // Arrange
        var updateRequest = new
        {
            quantity = 0,
            reason = "Out of stock"
        };
        var endpoint = ApiTestConstants.Endpoints.ProductStock.Replace("{id}", _testProductId.ToString());

        // Act
        var response = _client.Put(endpoint, updateRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
    }

    // NEGATIVE TESTS - UPDATE

    /// <summary>
    /// Test Case 6.5: Update Product with Negative Price
    /// Scenario: Attempt to set negative price
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void UpdateProduct_WithNegativePrice_Should_Return_400()
    {
        // Arrange
        var updateRequest = new
        {
            name = "Test Product",
            sku = "SKU-001",
            price = -10.00m,
            quantity = 50,
            category = "Test",
            supplierId = (int?)null
        };
        var endpoint = ApiTestConstants.Endpoints.ProductById.Replace("{id}", _testProductId.ToString());

        // Act
        var response = _client.Put(endpoint, updateRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
        Assert.Contains("price", response.Content?.ToLower() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 6.6: Update Product with Zero Price
    /// Scenario: Set price to zero
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void UpdateProduct_WithZeroPrice_Should_Return_400()
    {
        // Arrange
        var updateRequest = new
        {
            name = "Test Product",
            sku = "SKU-001",
            price = 0m,
            quantity = 50,
            category = "Test",
            supplierId = (int?)null
        };
        var endpoint = ApiTestConstants.Endpoints.ProductById.Replace("{id}", _testProductId.ToString());

        // Act
        var response = _client.Put(endpoint, updateRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 6.7: Update Product with Empty Name
    /// Scenario: Attempt to set empty product name
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void UpdateProduct_WithEmptyName_Should_Return_400()
    {
        // Arrange
        var updateRequest = new
        {
            name = "",
            sku = "SKU-001",
            price = 50.00m,
            quantity = 50,
            category = "Test",
            supplierId = (int?)null
        };
        var endpoint = ApiTestConstants.Endpoints.ProductById.Replace("{id}", _testProductId.ToString());

        // Act
        var response = _client.Put(endpoint, updateRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 6.8: Update Non-existent Product
    /// Scenario: Attempt to update product that doesn't exist
    /// Expected: Returns 404 Not Found
    /// </summary>
    [Fact]
    public void UpdateProduct_NonExistentId_Should_Return_404()
    {
        // Arrange
        var updateRequest = new
        {
            name = "Test Product",
            sku = "SKU-999",
            price = 50.00m,
            quantity = 50,
            category = "Test",
            supplierId = (int?)null
        };
        var endpoint = ApiTestConstants.Endpoints.ProductById.Replace("{id}", "99999");

        // Act
        var response = _client.Put(endpoint, updateRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.NotFound, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 6.9: Update Product Stock with Negative Quantity
    /// Scenario: Set negative stock quantity
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void UpdateProductStock_WithNegativeQuantity_Should_Return_400()
    {
        // Arrange
        var updateRequest = new
        {
            quantity = -50,
            reason = "Invalid adjustment"
        };
        var endpoint = ApiTestConstants.Endpoints.ProductStock.Replace("{id}", _testProductId.ToString());

        // Act
        var response = _client.Put(endpoint, updateRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
        Assert.Contains("negative", response.Content?.ToLower() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 6.10: Update Stock for Non-existent Product
    /// Scenario: Update stock for product ID that doesn't exist
    /// Expected: Returns 404 Not Found
    /// </summary>
    [Fact]
    public void UpdateProductStock_NonExistentId_Should_Return_404()
    {
        // Arrange
        var updateRequest = new
        {
            quantity = 100,
            reason = "Test"
        };
        var endpoint = ApiTestConstants.Endpoints.ProductStock.Replace("{id}", "99999");

        // Act
        var response = _client.Put(endpoint, updateRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.NotFound, (int)response.StatusCode);
    }

    // POSITIVE TESTS - DELETE

    /// <summary>
    /// Test Case 6.11: Delete Existing Product
    /// Scenario: Delete a product that exists
    /// Expected: Returns 200 OK
    /// </summary>
    [Fact]
    public void DeleteProduct_ExistingProduct_Should_Return_200()
    {
        // Arrange - Create a product to delete
        var uniqueSku = $"DEL-{Guid.NewGuid():N}";
        var productData = new
        {
            name = $"Delete Test Product {uniqueSku}",
            sku = uniqueSku,
            price = 29.99m,
            quantity = 50,
            category = "Test",
            supplierId = (int?)null
        };
        var createResponse = _client.Post(ApiTestConstants.Endpoints.Products, productData);
        
        var listResponse = _client.Get(ApiTestConstants.Endpoints.Products);
        using var listDoc = JsonDocument.Parse(listResponse.Content);
        int productIdToDelete = 0;
        foreach (var product in listDoc.RootElement.EnumerateArray())
        {
            string? sku = product.TryGetProperty("sku", out var skuElement) ? skuElement.GetString() : null;
            if (string.Equals(sku, uniqueSku, StringComparison.OrdinalIgnoreCase) &&
                product.TryGetProperty("id", out var idElement) && idElement.TryGetInt32(out var id))
            {
                productIdToDelete = id;
                break;
            }
        }

        var endpoint = ApiTestConstants.Endpoints.ProductById.Replace("{id}", productIdToDelete.ToString());

        // Act
        var response = _client.Delete(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.Contains("deleted successfully", response.Content ?? "", StringComparison.OrdinalIgnoreCase);
    }

    // NEGATIVE TESTS - DELETE

    /// <summary>
    /// Test Case 6.12: Delete Non-existent Product
    /// Scenario: Attempt to delete product that doesn't exist
    /// Expected: Returns 404 Not Found
    /// </summary>
    [Fact]
    public void DeleteProduct_NonExistentId_Should_Return_404()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ProductById.Replace("{id}", "99999");

        // Act
        var response = _client.Delete(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.NotFound, (int)response.StatusCode);
        Assert.Contains("not found", response.Content?.ToLower() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 6.13: Delete Product with Negative ID
    /// Scenario: Attempt to delete with invalid negative ID
    /// Expected: Returns 400 Bad Request or 404 Not Found
    /// </summary>
    [Fact]
    public void DeleteProduct_WithNegativeId_Should_ReturnError()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ProductById.Replace("{id}", "-1");

        // Act
        var response = _client.Delete(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.NotFound
        );
    }

    /// <summary>
    /// Test Case 6.14: Delete Product with Invalid ID Format
    /// Scenario: Use non-numeric ID format
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void DeleteProduct_WithInvalidIdFormat_Should_Return_400()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ProductById.Replace("{id}", "abc123");

        // Act
        var response = _client.Delete(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 6.15: Update Product Name with Very Long Value
    /// Scenario: Update product name with extremely long string (>255 chars)
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void UpdateProduct_WithVeryLongName_Should_Return_400()
    {
        // Arrange
        var longName = new string('A', 300);
        var updateRequest = new
        {
            name = longName,
            sku = "SKU-001",
            price = 50.00m,
            quantity = 50,
            category = "Test",
            supplierId = (int?)null
        };
        var endpoint = ApiTestConstants.Endpoints.ProductById.Replace("{id}", _testProductId.ToString());

        // Act
        var response = _client.Put(endpoint, updateRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }
}
