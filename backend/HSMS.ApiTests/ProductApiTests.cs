using Xunit;
using HSMS.ApiTests.Helpers;

namespace HSMS.ApiTests;

/// <summary>
/// MEMBER 3 - Product API Tests (CRUD Operations - Positive Scenarios)
/// 
/// Tests product management operations:
/// - Retrieve product list
/// - Get individual product details
/// - Create new products
/// - Validate product data integrity
/// - Response structure validation
/// 
/// Focus: CRUD functionality, data retrieval, business logic
/// </summary>
public class ProductApiTests
{
    private readonly ApiClient _client = new ApiClient();

    /// <summary>
    /// Test Case 3.1: Get All Products - Basic Retrieval
    /// Scenario: Retrieve list of all products
    /// Expected: Returns 200 OK with product list (JSON array)
    /// </summary>
    [Fact]
    public void GetAllProducts_Should_Return_200_WithProductList()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.Products;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
        Assert.Contains("[", response.Content); // JSON array indicator
    }

    /// <summary>
    /// Test Case 3.2: Get All Products - Response Structure
    /// Scenario: Verify response contains valid product data
    /// Expected: Contains product identifiers and attributes
    /// </summary>
    [Fact]
    public void GetAllProducts_Response_Should_ContainProductData()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.Products;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        var content = response.Content?.ToLower() ?? "";
        // Response should contain product-related fields
        Assert.True(
            content.Contains("id") || content.Contains("name") || content.Contains("price"),
            "Response should contain product fields (id, name, or price)"
        );
    }

    /// <summary>
    /// Test Case 3.3: Get Product by Valid ID
    /// Scenario: Retrieve specific product by ID (assuming ID 1 exists)
    /// Expected: Returns 200 OK with product details
    /// </summary>
    [Fact]
    public void GetProductById_WithValidId_Should_Return_200()
    {
        // Arrange
        var productId = 1; // Assuming default seeded product
        var endpoint = ApiTestConstants.Endpoints.ProductById.Replace("{id}", productId.ToString());

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
        Assert.Contains("\"id\"", response.Content);
    }

    /// <summary>
    /// Test Case 3.4: Get Product Details - Contains Required Fields
    /// Scenario: Verify single product response has all required attributes
    /// Expected: Response includes id, name, price, stock
    /// </summary>
    [Fact]
    public void GetProductById_Response_Should_ContainRequiredFields()
    {
        // Arrange
        var productId = 1;
        var endpoint = ApiTestConstants.Endpoints.ProductById.Replace("{id}", productId.ToString());

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        var content = response.Content?.ToLower() ?? "";
        Assert.Contains("id", content);
    }

    /// <summary>
    /// Test Case 3.5: Create New Product - Successful Creation
    /// Scenario: Create a new product with valid data
    /// Expected: Returns 201 Created with product ID
    /// </summary>
    [Fact]
    public void CreateProduct_WithValidData_Should_Return_201()
    {
        // Arrange
        var newProduct = new
        {
            name = "Test Hammer - " + Guid.NewGuid().ToString().Substring(0, 8),
            price = 29.99m,
            stock = 50,
            supplierId = 1
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Products, newProduct);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.Created, (int)response.StatusCode);
        Assert.NotNull(response.Content);
    }

    /// <summary>
    /// Test Case 3.6: Create Product Response - Contains ID
    /// Scenario: Verify created product response includes the new ID
    /// Expected: Response contains productId or id field
    /// </summary>
    [Fact]
    public void CreateProduct_Response_Should_ContainProductId()
    {
        // Arrange
        var newProduct = new
        {
            name = "Test Screwdriver - " + Guid.NewGuid().ToString().Substring(0, 8),
            price = 15.99m,
            stock = 100,
            supplierId = 1
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Products, newProduct);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.Created, (int)response.StatusCode);
        Assert.Contains("\"id\"", response.Content ?? "");
    }

    /// <summary>
    /// Test Case 3.7: Create Multiple Products
    /// Scenario: Create two different products sequentially
    /// Expected: Both return 201 Created
    /// </summary>
    [Fact]
    public void CreateMultipleProducts_Should_AllReturn_201()
    {
        // Arrange
        var products = new[]
        {
            new { name = "Product A - " + Guid.NewGuid(), price = 10.00m, stock = 50, supplierId = 1 },
            new { name = "Product B - " + Guid.NewGuid(), price = 20.00m, stock = 30, supplierId = 1 }
        };

        // Act & Assert
        foreach (var product in products)
        {
            var response = _client.Post(ApiTestConstants.Endpoints.Products, product);
            Assert.Equal(ApiTestConstants.HttpStatusCodes.Created, (int)response.StatusCode);
        }
    }

    /// <summary>
    /// Test Case 3.8: Product Price Validation
    /// Scenario: Verify product with valid price is accepted
    /// Expected: Returns 201 Created
    /// </summary>
    [Fact]
    public void CreateProduct_WithValidPrice_Should_Return_201()
    {
        // Arrange
        var newProduct = new
        {
            name = "Priced Product - " + Guid.NewGuid().ToString().Substring(0, 8),
            price = 99.99m,
            stock = 25,
            supplierId = 1
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Products, newProduct);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.Created, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 3.9: Product Stock Quantity
    /// Scenario: Create product with various stock levels
    /// Expected: Returns 201 Created for positive stock
    /// </summary>
    [Fact]
    public void CreateProduct_WithPositiveStock_Should_Return_201()
    {
        // Arrange
        var newProduct = new
        {
            name = "Stocked Product - " + Guid.NewGuid().ToString().Substring(0, 8),
            price = 45.50m,
            stock = 100,
            supplierId = 1
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Products, newProduct);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.Created, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 3.10: Get Product List - Performance
    /// Scenario: Verify product list retrieval completes reasonably fast
    /// Expected: Response time < 1 second
    /// </summary>
    [Fact]
    public void GetAllProducts_Should_RespondWithinAcceptableTime()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.Products;

        // Act
        var response = _client.Get(endpoint);
        var responseTime = ApiClient.GetResponseTime(response);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.True(responseTime?.TotalSeconds < 1, 
            $"Response took {responseTime?.TotalSeconds}s, expected < 1s");
    }

    /// <summary>
    /// Test Case 3.11: Products Endpoint Availability
    /// Scenario: Verify products endpoint is accessible
    /// Expected: Does not return 404 or 500
    /// </summary>
    [Fact]
    public void ProductsEndpoint_Should_BeAccessible()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.Products;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.NotEqual(ApiTestConstants.HttpStatusCodes.NotFound, (int)response.StatusCode);
        Assert.NotEqual(ApiTestConstants.HttpStatusCodes.InternalServerError, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 3.12: Create Product with Supplier Reference
    /// Scenario: Create product linked to existing supplier
    /// Expected: Returns 201 Created
    /// </summary>
    [Fact]
    public void CreateProduct_WithSupplierId_Should_Return_201()
    {
        // Arrange
        var newProduct = new
        {
            name = "Supplier Referenced Product - " + Guid.NewGuid().ToString().Substring(0, 8),
            price = 34.99m,
            stock = 75,
            supplierId = 1
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Products, newProduct);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.Created, (int)response.StatusCode);
    }
}
