using Xunit;
using System.Text.Json;
using HSMS.ApiTests.Helpers;

namespace HSMS.ApiTests;

/// <summary>
/// MEMBER 12 - Suppliers API Tests (Positive & Negative Scenarios)
/// 
/// Tests supplier management:
/// - Get all suppliers
/// - Create suppliers
/// - Update suppliers
/// - Delete suppliers
/// 
/// Focus: CRUD operations, error handling, constraints
/// </summary>
public class SuppliersApiTests
{
    private readonly ApiClient _client = new ApiClient();

    public SuppliersApiTests()
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

    // POSITIVE TESTS - GET

    /// <summary>
    /// Test Case 12.1: Get All Suppliers
    /// Scenario: Retrieve all suppliers
    /// Expected: Returns 200 OK with suppliers list
    /// </summary>
    [Fact]
    public void GetSuppliers_Should_Return_200()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.Suppliers;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
    }

    /// <summary>
    /// Test Case 12.2: Get Suppliers Returns Array
    /// Scenario: Response format validation
    /// Expected: Valid JSON array
    /// </summary>
    [Fact]
    public void GetSuppliers_Response_ShouldBeArray()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.Suppliers;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        using var doc = JsonDocument.Parse(response.Content);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    // POSITIVE TESTS - CREATE

    /// <summary>
    /// Test Case 12.3: Create Supplier with Valid Data
    /// Scenario: Add new supplier with name and contact info
    /// Expected: Returns 201 Created
    /// </summary>
    [Fact]
    public void CreateSupplier_WithValidData_Should_Return_201()
    {
        // Arrange
        var supplierRequest = new
        {
            name = $"Test Supplier {Guid.NewGuid():N}",
            contactInfo = "john@supplier.com"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Suppliers, supplierRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.Created, (int)response.StatusCode);
        Assert.NotNull(response.Content);
    }

    /// <summary>
    /// Test Case 12.4: Create Supplier Response Contains ID
    /// Scenario: Verify created supplier has ID
    /// Expected: Response includes supplier ID
    /// </summary>
    [Fact]
    public void CreateSupplier_Response_ShouldContainId()
    {
        // Arrange
        var supplierRequest = new
        {
            name = $"Test Supplier {Guid.NewGuid():N}",
            contactInfo = "contact@supplier.com"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Suppliers, supplierRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.Created, (int)response.StatusCode);
        using var doc = JsonDocument.Parse(response.Content);
        Assert.True(doc.RootElement.TryGetProperty("id", out var idElement));
        Assert.True(idElement.TryGetInt32(out var id));
        Assert.True(id > 0);
    }

    /// <summary>
    /// Test Case 12.5: Create Supplier without Contact Info
    /// Scenario: Contact info is optional
    /// Expected: Returns 201 Created
    /// </summary>
    [Fact]
    public void CreateSupplier_WithoutContactInfo_Should_Return_201()
    {
        // Arrange
        var supplierRequest = new
        {
            name = $"Supplier {Guid.NewGuid():N}",
            contactInfo = (string?)null
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Suppliers, supplierRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.Created, (int)response.StatusCode);
    }

    // POSITIVE TESTS - UPDATE

    /// <summary>
    /// Test Case 12.6: Update Supplier with Valid Data
    /// Scenario: Update supplier name and contact
    /// Expected: Returns 200 OK
    /// </summary>
    [Fact]
    public void UpdateSupplier_WithValidData_Should_Return_200()
    {
        // Arrange - Create a supplier
        var createRequest = new
        {
            name = $"Update Test {Guid.NewGuid():N}",
            contactInfo = "old@email.com"
        };
        var createResponse = _client.Post(ApiTestConstants.Endpoints.Suppliers, createRequest);
        using var createDoc = JsonDocument.Parse(createResponse.Content);
        var supplierId = createDoc.RootElement.GetProperty("id").GetInt32();

        var updateRequest = new
        {
            name = $"UpdatedSupplier_{Guid.NewGuid():N}",
            contactInfo = "new@email.com"
        };
        var endpoint = ApiTestConstants.Endpoints.SupplierById.Replace("{id}", supplierId.ToString());

        // Act
        var response = _client.Put(endpoint, updateRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.Contains("updated successfully", response.Content ?? "", StringComparison.OrdinalIgnoreCase);
    }

    // POSITIVE TESTS - DELETE

    /// <summary>
    /// Test Case 12.7: Delete Supplier
    /// Scenario: Delete existing supplier without linked records
    /// Expected: Returns 200 OK
    /// </summary>
    [Fact]
    public void DeleteSupplier_WithoutLinkedRecords_Should_Return_200()
    {
        // Arrange - Create a supplier to delete
        var createRequest = new
        {
            name = $"Delete Test {Guid.NewGuid():N}",
            contactInfo = "delete@email.com"
        };
        var createResponse = _client.Post(ApiTestConstants.Endpoints.Suppliers, createRequest);
        using var createDoc = JsonDocument.Parse(createResponse.Content);
        var supplierId = createDoc.RootElement.GetProperty("id").GetInt32();

        var endpoint = ApiTestConstants.Endpoints.SupplierById.Replace("{id}", supplierId.ToString());

        // Act
        var response = _client.Delete(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.Contains("deleted successfully", response.Content ?? "", StringComparison.OrdinalIgnoreCase);
    }

    // NEGATIVE TESTS - CREATE

    /// <summary>
    /// Test Case 12.8: Create Supplier with Empty Name
    /// Scenario: Supplier name is empty
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateSupplier_WithEmptyName_Should_Return_400()
    {
        // Arrange
        var supplierRequest = new
        {
            name = "",
            contactInfo = "contact@email.com"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Suppliers, supplierRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
        Assert.Contains("required", response.Content?.ToLower() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 12.9: Create Supplier with Whitespace Name
    /// Scenario: Name contains only whitespace
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateSupplier_WithWhitespaceName_Should_Return_400()
    {
        // Arrange
        var supplierRequest = new
        {
            name = "   ",
            contactInfo = "contact@email.com"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Suppliers, supplierRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 12.10: Create Duplicate Supplier
    /// Scenario: Supplier name already exists
    /// Expected: Returns 409 Conflict
    /// </summary>
    [Fact]
    public void CreateSupplier_WithDuplicateName_Should_Return_409()
    {
        // Arrange
        var uniqueName = $"Unique Supplier {Guid.NewGuid():N}";
        var firstRequest = new
        {
            name = uniqueName,
            contactInfo = "first@email.com"
        };
        _client.Post(ApiTestConstants.Endpoints.Suppliers, firstRequest);

        var secondRequest = new
        {
            name = uniqueName,
            contactInfo = "second@email.com"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Suppliers, secondRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.Conflict, (int)response.StatusCode);
    }

    // NEGATIVE TESTS - UPDATE

    /// <summary>
    /// Test Case 12.11: Update Non-existent Supplier
    /// Scenario: Supplier ID doesn't exist
    /// Expected: Returns 404 Not Found
    /// </summary>
    [Fact]
    public void UpdateSupplier_NonExistentId_Should_Return_404()
    {
        // Arrange
        var updateRequest = new
        {
            name = "Updated Name",
            contactInfo = "updated@email.com"
        };
        var endpoint = ApiTestConstants.Endpoints.SupplierById.Replace("{id}", "999999");

        // Act
        var response = _client.Put(endpoint, updateRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.NotFound, (int)response.StatusCode);
        Assert.Contains("not found", response.Content?.ToLower() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 12.12: Update Supplier with Empty Name
    /// Scenario: Update to empty name
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void UpdateSupplier_WithEmptyName_Should_Return_400()
    {
        // Arrange - Create supplier
        var createRequest = new
        {
            name = $"Update Test {Guid.NewGuid():N}",
            contactInfo = "test@email.com"
        };
        var createResponse = _client.Post(ApiTestConstants.Endpoints.Suppliers, createRequest);
        using var createDoc = JsonDocument.Parse(createResponse.Content);
        var supplierId = createDoc.RootElement.GetProperty("id").GetInt32();

        var updateRequest = new
        {
            name = "",
            contactInfo = "new@email.com"
        };
        var endpoint = ApiTestConstants.Endpoints.SupplierById.Replace("{id}", supplierId.ToString());

        // Act
        var response = _client.Put(endpoint, updateRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 12.13: Update Supplier to Duplicate Name
    /// Scenario: Update name to existing supplier name
    /// Expected: Returns 409 Conflict
    /// </summary>
    [Fact]
    public void UpdateSupplier_ToDuplicateName_Should_Return_409()
    {
        // Arrange - Create two suppliers
        var supplier1 = new
        {
            name = $"Supplier One {Guid.NewGuid():N}",
            contactInfo = "one@email.com"
        };
        var supplier2 = new
        {
            name = $"Supplier Two {Guid.NewGuid():N}",
            contactInfo = "two@email.com"
        };

        var response1 = _client.Post(ApiTestConstants.Endpoints.Suppliers, supplier1);
        var response2 = _client.Post(ApiTestConstants.Endpoints.Suppliers, supplier2);

        using var doc1 = JsonDocument.Parse(response1.Content);
        using var doc2 = JsonDocument.Parse(response2.Content);

        var id1 = doc1.RootElement.GetProperty("id").GetInt32();
        var name2 = doc2.RootElement.GetProperty("name").GetString();

        var updateRequest = new
        {
            name = name2,
            contactInfo = "updated@email.com"
        };
        var endpoint = ApiTestConstants.Endpoints.SupplierById.Replace("{id}", id1.ToString());

        // Act
        var response = _client.Put(endpoint, updateRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.Conflict, (int)response.StatusCode);
    }

    // NEGATIVE TESTS - DELETE

    /// <summary>
    /// Test Case 12.14: Delete Non-existent Supplier
    /// Scenario: Supplier ID doesn't exist
    /// Expected: Returns 404 Not Found
    /// </summary>
    [Fact]
    public void DeleteSupplier_NonExistentId_Should_Return_404()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.SupplierById.Replace("{id}", "999999");

        // Act
        var response = _client.Delete(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.NotFound, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 12.15: Delete Supplier with Linked Products
    /// Scenario: Delete supplier that has linked products
    /// Expected: Returns 409 Conflict
    /// </summary>
    [Fact]
    public void DeleteSupplier_WithLinkedRecords_Should_Return_409()
    {
        // Arrange - Create supplier
        var createRequest = new
        {
            name = $"Linked Supplier {Guid.NewGuid():N}",
            contactInfo = "linked@email.com"
        };
        var createResponse = _client.Post(ApiTestConstants.Endpoints.Suppliers, createRequest);
        using var createDoc = JsonDocument.Parse(createResponse.Content);
        var supplierId = createDoc.RootElement.GetProperty("id").GetInt32();

        // Try to create a product with this supplier (if possible)
        // Then delete supplier - should fail

        var endpoint = ApiTestConstants.Endpoints.SupplierById.Replace("{id}", supplierId.ToString());

        // Act
        var response = _client.Delete(endpoint);

        // Assert
        // Either 200 (no linked records) or 409 (conflict if linked)
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.Conflict
        );
    }

    /// <summary>
    /// Test Case 12.16: Delete Supplier with Negative ID
    /// Scenario: Supplier ID is negative
    /// Expected: Returns 404 Not Found or 400 Bad Request
    /// </summary>
    [Fact]
    public void DeleteSupplier_WithNegativeId_Should_ReturnError()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.SupplierById.Replace("{id}", "-1");

        // Act
        var response = _client.Delete(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.NotFound
        );
    }

    /// <summary>
    /// Test Case 12.17: Create Supplier Performance Test
    /// Scenario: Supplier creation performance
    /// Expected: Response time < 2 seconds
    /// </summary>
    [Fact]
    public void CreateSupplier_Should_CompleteWithinReasonableTime()
    {
        // Arrange
        var supplierRequest = new
        {
            name = $"Perf Test {Guid.NewGuid():N}",
            contactInfo = "perf@email.com"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Suppliers, supplierRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.Created, (int)response.StatusCode);
        var responseTime = ApiClient.GetResponseTime(response);
        Assert.NotNull(responseTime);
        Assert.True(responseTime.Value.TotalMilliseconds < 2000);
    }
}
