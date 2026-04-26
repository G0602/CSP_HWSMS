using Xunit;
using HSMS.ApiTests.Helpers;

namespace HSMS.ApiTests;

/// <summary>
/// MEMBER 4 - Product API Tests (Edge Cases & Negative Scenarios)
/// 
/// Tests error conditions and boundary cases:
/// - Invalid product IDs
/// - Out of stock scenarios
/// - Invalid input data
/// - Missing required fields
/// - Boundary conditions
/// - Data validation
/// 
/// Focus: Error handling, data validation, edge cases
/// </summary>
public class ProductApiNegativeTests
{
    private readonly ApiClient _client = new ApiClient();

    /// <summary>
    /// Test Case 4.1: Get Product with Invalid ID
    /// Scenario: Request product with non-existent ID (e.g., 99999)
    /// Expected: Returns 404 Not Found
    /// </summary>
    [Fact]
    public void GetProductById_WithInvalidId_Should_Return_404()
    {
        // Arrange
        var invalidProductId = 99999;
        var endpoint = ApiTestConstants.Endpoints.ProductById.Replace("{id}", invalidProductId.ToString());

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.NotFound, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 4.2: Get Product with Negative ID
    /// Scenario: Request product with negative ID
    /// Expected: Returns 404 or 400
    /// </summary>
    [Fact]
    public void GetProductById_WithNegativeId_Should_Return_400Or404()
    {
        // Arrange
        var negativeId = -1;
        var endpoint = ApiTestConstants.Endpoints.ProductById.Replace("{id}", negativeId.ToString());

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.NotFound,
            "Negative ID should return 400 or 404"
        );
    }

    /// <summary>
    /// Test Case 4.3: Get Product with Zero ID
    /// Scenario: Request product with ID = 0
    /// Expected: Returns 404 or 400
    /// </summary>
    [Fact]
    public void GetProductById_WithZeroId_Should_Return_400Or404()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ProductById.Replace("{id}", "0");

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.NotFound,
            "ID 0 should return 400 or 404"
        );
    }

    /// <summary>
    /// Test Case 4.4: Get Product with Non-Numeric ID
    /// Scenario: Request product with non-numeric ID string
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void GetProductById_WithNonNumericId_Should_Return_400()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ProductById.Replace("{id}", "invalid");

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 4.5: Create Product with Missing Name
    /// Scenario: Create product without name field
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateProduct_WithoutName_Should_Return_400()
    {
        // Arrange
        var invalidProduct = new
        {
            price = 25.99m,
            stock = 50,
            supplierId = 1
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Products, invalidProduct);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 4.6: Create Product with Empty Name
    /// Scenario: Create product with empty string name
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateProduct_WithEmptyName_Should_Return_400()
    {
        // Arrange
        var invalidProduct = new
        {
            name = "",
            price = 25.99m,
            stock = 50,
            supplierId = 1
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Products, invalidProduct);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 4.7: Create Product with Missing Price
    /// Scenario: Create product without price field
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateProduct_WithoutPrice_Should_Return_400()
    {
        // Arrange
        var invalidProduct = new
        {
            name = "Product Without Price",
            stock = 50,
            supplierId = 1
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Products, invalidProduct);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 4.8: Create Product with Negative Price
    /// Scenario: Create product with negative price value
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateProduct_WithNegativePrice_Should_Return_400()
    {
        // Arrange
        var invalidProduct = new
        {
            name = "Product With Negative Price",
            price = -10.00m,
            stock = 50,
            supplierId = 1
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Products, invalidProduct);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 4.9: Create Product with Zero Price
    /// Scenario: Create product with price = 0
    /// Expected: Returns 400 Bad Request (free products not allowed)
    /// </summary>
    [Fact]
    public void CreateProduct_WithZeroPrice_Should_Return_400()
    {
        // Arrange
        var invalidProduct = new
        {
            name = "Free Product - " + Guid.NewGuid().ToString().Substring(0, 8),
            price = 0.00m,
            stock = 50,
            supplierId = 1
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Products, invalidProduct);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 4.10: Create Product with Missing Stock
    /// Scenario: Create product without stock field
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateProduct_WithoutStock_Should_Return_400()
    {
        // Arrange
        var invalidProduct = new
        {
            name = "Product Without Stock",
            price = 25.99m,
            supplierId = 1
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Products, invalidProduct);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 4.11: Create Product with Negative Stock
    /// Scenario: Create product with negative stock quantity
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateProduct_WithNegativeStock_Should_Return_400()
    {
        // Arrange
        var invalidProduct = new
        {
            name = "Product With Negative Stock",
            price = 25.99m,
            stock = -10,
            supplierId = 1
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Products, invalidProduct);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 4.12: Create Product with Whitespace Only Name
    /// Scenario: Product name contains only spaces
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateProduct_WithWhitespaceOnlyName_Should_Return_400()
    {
        // Arrange
        var invalidProduct = new
        {
            name = "     ",
            price = 25.99m,
            stock = 50,
            supplierId = 1
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Products, invalidProduct);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 4.13: Create Product with Very Long Name
    /// Scenario: Product name exceeds reasonable length (e.g., 1000 chars)
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateProduct_WithExcessivelyLongName_Should_Return_400()
    {
        // Arrange
        var longName = new string('a', 1000);
        var invalidProduct = new
        {
            name = longName,
            price = 25.99m,
            stock = 50,
            supplierId = 1
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Products, invalidProduct);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 4.14: Create Product with Invalid Supplier ID
    /// Scenario: Reference non-existent supplier
    /// Expected: Returns 400 or 409
    /// </summary>
    [Fact]
    public void CreateProduct_WithInvalidSupplierId_Should_Return_Error()
    {
        // Arrange
        var invalidProduct = new
        {
            name = "Product With Invalid Supplier - " + Guid.NewGuid().ToString().Substring(0, 8),
            price = 25.99m,
            stock = 50,
            supplierId = 99999 // Non-existent supplier
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Products, invalidProduct);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.Conflict,
            "Invalid supplier should return error"
        );
    }

    /// <summary>
    /// Test Case 4.15: Create Product with Maximum Safe Price
    /// Scenario: Create product with very high price (boundary test)
    /// Expected: Should either accept or reject gracefully (not 500)
    /// </summary>
    [Fact]
    public void CreateProduct_WithVeryHighPrice_Should_NotCrash()
    {
        // Arrange
        var expensiveProduct = new
        {
            name = "Expensive Product - " + Guid.NewGuid().ToString().Substring(0, 8),
            price = 999999.99m,
            stock = 1,
            supplierId = 1
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Products, expensiveProduct);

        // Assert - Should not crash with 500 error
        Assert.NotEqual(ApiTestConstants.HttpStatusCodes.InternalServerError, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 4.16: Create Product with Maximum Safe Stock
    /// Scenario: Create product with very high stock quantity
    /// Expected: Should either accept or reject gracefully (not 500)
    /// </summary>
    [Fact]
    public void CreateProduct_WithVeryHighStock_Should_NotCrash()
    {
        // Arrange
        var highStockProduct = new
        {
            name = "High Stock Product - " + Guid.NewGuid().ToString().Substring(0, 8),
            price = 25.99m,
            stock = 1000000,
            supplierId = 1
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Products, highStockProduct);

        // Assert - Should not crash with 500 error
        Assert.NotEqual(ApiTestConstants.HttpStatusCodes.InternalServerError, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 4.17: SQL Injection in Product Name
    /// Scenario: Product name contains SQL injection payload
    /// Expected: Should be treated as literal string, not executed
    /// </summary>
    [Fact]
    public void CreateProduct_WithSqlInjectionInName_Should_NotExecute()
    {
        // Arrange
        var maliciousProduct = new
        {
            name = "'; DROP TABLE products; --",
            price = 25.99m,
            stock = 50,
            supplierId = 1
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Products, maliciousProduct);

        // Assert - Should reject or treat as literal, not crash
        Assert.True(
            (int)response.StatusCode != ApiTestConstants.HttpStatusCodes.InternalServerError,
            "SQL injection should not crash the server"
        );
    }

    /// <summary>
    /// Test Case 4.18: Special Characters in Product Name
    /// Scenario: Product name contains special characters
    /// Expected: Should be accepted or rejected gracefully
    /// </summary>
    [Fact]
    public void CreateProduct_WithSpecialCharactersInName_Should_HandleGracefully()
    {
        // Arrange
        var specialCharProduct = new
        {
            name = "Product @#$%^&*()[]{}|\\<>? - " + Guid.NewGuid().ToString().Substring(0, 8),
            price = 25.99m,
            stock = 50,
            supplierId = 1
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Products, specialCharProduct);

        // Assert - Should not crash server
        Assert.NotEqual(ApiTestConstants.HttpStatusCodes.InternalServerError, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 4.19: Create Product with Duplicate Name
    /// Scenario: Create product with name that already exists
    /// Expected: Returns 409 Conflict or allows it
    /// </summary>
    [Fact]
    public void CreateProduct_WithDuplicateName_Should_HandleAppropriately()
    {
        // Arrange
        var duplicateName = "Unique Product - " + Guid.NewGuid().ToString();
        var product1 = new { name = duplicateName, price = 25.99m, stock = 50, supplierId = 1 };
        var product2 = new { name = duplicateName, price = 30.00m, stock = 40, supplierId = 1 };

        // Act
        var response1 = _client.Post(ApiTestConstants.Endpoints.Products, product1);
        var response2 = _client.Post(ApiTestConstants.Endpoints.Products, product2);

        // Assert - Either both succeed or second fails gracefully
        Assert.True(
            (int)response1.StatusCode == ApiTestConstants.HttpStatusCodes.Created ||
            (int)response2.StatusCode == ApiTestConstants.HttpStatusCodes.Conflict,
            "Duplicate name should be handled appropriately"
        );
    }
}
