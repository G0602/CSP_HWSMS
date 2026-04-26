using Xunit;
using System.Text.Json;
using HSMS.ApiTests.Helpers;

namespace HSMS.ApiTests;

/// <summary>
/// MEMBER 5 - Authentication Register API Tests
/// 
/// Tests user registration workflows:
/// - Successful registration with valid data
/// - Registration with different roles
/// - Response validation
/// - Error handling for invalid input
/// 
/// Focus: Registration functionality, input validation, role assignment
/// </summary>
public class AuthRegisterTests
{
    private readonly ApiClient _client = new ApiClient();

    /// <summary>
    /// Test Case 5.1: Valid Registration with Admin Role
    /// Scenario: Register new user with admin role
    /// Expected: Returns 200 OK with access token and user data
    /// </summary>
    [Fact]
    public void Register_WithValidAdminData_Should_Return_200_AndToken()
    {
        // Arrange
        var uniqueUsername = $"adminuser_{Guid.NewGuid():N}";
        var registerRequest = new
        {
            username = uniqueUsername,
            password = "SecurePass@123",
            role = "Admin"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthRegister, registerRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
        Assert.Contains("accessToken", response.Content);
        Assert.Contains("userId", response.Content);
        Assert.Contains(uniqueUsername, response.Content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 5.2: Valid Registration with Manager Role
    /// Scenario: Register new user with manager role
    /// Expected: Returns 200 OK with token and correct role
    /// </summary>
    [Fact]
    public void Register_WithValidManagerData_Should_Return_200_AndToken()
    {
        // Arrange
        var uniqueUsername = $"manager_{Guid.NewGuid():N}";
        var registerRequest = new
        {
            username = uniqueUsername,
            password = "SecurePass@123",
            role = "Manager"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthRegister, registerRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
        Assert.Contains("accessToken", response.Content);
        Assert.Contains("Manager", response.Content);
    }

    /// <summary>
    /// Test Case 5.3: Valid Registration with Cashier Role
    /// Scenario: Register new user with cashier role
    /// Expected: Returns 200 OK
    /// </summary>
    [Fact]
    public void Register_WithValidCashierData_Should_Return_200()
    {
        // Arrange
        var uniqueUsername = $"cashier_{Guid.NewGuid():N}";
        var registerRequest = new
        {
            username = uniqueUsername,
            password = "SecurePass@123",
            role = "Cashier"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthRegister, registerRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
        Assert.Contains("accessToken", response.Content);
    }

    /// <summary>
    /// Test Case 5.4: Registration with Default Role
    /// Scenario: Register without specifying role (should default to Cashier)
    /// Expected: Returns 200 OK with Cashier role
    /// </summary>
    [Fact]
    public void Register_WithoutRole_Should_DefaultToCashier()
    {
        // Arrange
        var uniqueUsername = $"defaultuser_{Guid.NewGuid():N}";
        var registerRequest = new
        {
            username = uniqueUsername,
            password = "SecurePass@123"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthRegister, registerRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
        Assert.Contains("Cashier", response.Content);
    }

    /// <summary>
    /// Test Case 5.5: Registration Response Contains Valid Token
    /// Scenario: Verify token format and content
    /// Expected: Token is non-empty and contains JWT structure
    /// </summary>
    [Fact]
    public void Register_Response_Should_ContainValidToken()
    {
        // Arrange
        var uniqueUsername = $"tokentest_{Guid.NewGuid():N}";
        var registerRequest = new
        {
            username = uniqueUsername,
            password = "SecurePass@123",
            role = "Admin"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthRegister, registerRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        
        using var document = JsonDocument.Parse(response.Content);
        var token = document.RootElement.GetProperty("accessToken").GetString();
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        // JWT tokens have 3 parts separated by dots
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    // NEGATIVE TESTS

    /// <summary>
    /// Test Case 5.6: Register with Empty Username
    /// Scenario: Attempt registration without username
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void Register_WithEmptyUsername_Should_Return_400()
    {
        // Arrange
        var registerRequest = new
        {
            username = "",
            password = "SecurePass@123",
            role = "Admin"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthRegister, registerRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
        Assert.Contains("username", response.Content?.ToLower() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 5.7: Register with Whitespace-only Username
    /// Scenario: Username contains only whitespace
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void Register_WithWhitespaceUsername_Should_Return_400()
    {
        // Arrange
        var registerRequest = new
        {
            username = "   ",
            password = "SecurePass@123",
            role = "Admin"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthRegister, registerRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 5.8: Register with Duplicate Username
    /// Scenario: Username already exists
    /// Expected: Returns 409 Conflict
    /// </summary>
    [Fact]
    public void Register_WithDuplicateUsername_Should_Return_409()
    {
        // Arrange
        var uniqueUsername = $"duplicate_{Guid.NewGuid():N}";
        var firstRegister = new
        {
            username = uniqueUsername,
            password = "SecurePass@123",
            role = "Admin"
        };

        // Act - Register once
        _client.Post(ApiTestConstants.Endpoints.AuthRegister, firstRegister);

        // Act - Attempt to register with same username
        var secondRegister = new
        {
            username = uniqueUsername,
            password = "DifferentPass@123",
            role = "Manager"
        };
        var response = _client.Post(ApiTestConstants.Endpoints.AuthRegister, secondRegister);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.Conflict, (int)response.StatusCode);
        Assert.Contains("already taken", response.Content ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 5.9: Register with Short Password
    /// Scenario: Password less than 8 characters
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void Register_WithShortPassword_Should_Return_400()
    {
        // Arrange
        var registerRequest = new
        {
            username = $"user_{Guid.NewGuid():N}",
            password = "Short1!",
            role = "Admin"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthRegister, registerRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
        Assert.Contains("password", response.Content?.ToLower() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 5.10: Register with Empty Password
    /// Scenario: Password is empty
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void Register_WithEmptyPassword_Should_Return_400()
    {
        // Arrange
        var registerRequest = new
        {
            username = $"user_{Guid.NewGuid():N}",
            password = "",
            role = "Admin"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthRegister, registerRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 5.11: Register with Invalid Role
    /// Scenario: Role is not Admin, Manager, or Cashier
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void Register_WithInvalidRole_Should_Return_400()
    {
        // Arrange
        var registerRequest = new
        {
            username = $"user_{Guid.NewGuid():N}",
            password = "SecurePass@123",
            role = "SuperAdmin"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthRegister, registerRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
        Assert.Contains("role", response.Content?.ToLower() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 5.12: Register with Null Password
    /// Scenario: Password field is missing
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void Register_WithNullPassword_Should_Return_400()
    {
        // Arrange
        var registerRequest = new
        {
            username = $"user_{Guid.NewGuid():N}",
            password = (string?)null,
            role = "Admin"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthRegister, registerRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 5.13: Register with SQL Injection Attempt in Username
    /// Scenario: Username contains SQL injection payload
    /// Expected: Returns 200 OK (username accepted as string) or 400 Bad Request
    /// </summary>
    [Fact]
    public void Register_WithSqlInjectionInUsername_Should_RejectOrSanitize()
    {
        // Arrange
        var registerRequest = new
        {
            username = "user' OR '1'='1",
            password = "SecurePass@123",
            role = "Admin"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthRegister, registerRequest);

        // Assert
        // Either rejected or safely stored without SQL injection side effects
        // Accept any non-500 response as backend should handle gracefully
        Assert.NotEqual(ApiTestConstants.HttpStatusCodes.InternalServerError, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 5.14: Register Performance Test
    /// Scenario: Registration completes within reasonable time
    /// Expected: Response time < 2 seconds
    /// </summary>
    [Fact]
    public void Register_Should_CompleteWithinReasonableTime()
    {
        // Arrange
        var registerRequest = new
        {
            username = $"perftest_{Guid.NewGuid():N}",
            password = "SecurePass@123",
            role = "Admin"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthRegister, registerRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        var responseTime = ApiClient.GetResponseTime(response);
        Assert.NotNull(responseTime);
        Assert.True(responseTime.Value.TotalMilliseconds < 2000, 
            $"Registration took {responseTime.Value.TotalMilliseconds}ms, expected < 2000ms");
    }
}
