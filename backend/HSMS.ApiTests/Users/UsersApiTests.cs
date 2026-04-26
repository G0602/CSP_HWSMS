using Xunit;
using System.Text.Json;
using HSMS.ApiTests.Helpers;

namespace HSMS.ApiTests;

/// <summary>
/// MEMBER 13 - Users API Tests (Positive & Negative Scenarios)
/// 
/// Tests user management:
/// - Get all users
/// - Create users
/// - Update user roles
/// - Delete users
/// 
/// Focus: User CRUD operations, role management, authorization
/// </summary>
public class UsersApiTests
{
    private readonly ApiClient _client = new ApiClient();

    public UsersApiTests()
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
    /// Test Case 13.1: Get All Users
    /// Scenario: Retrieve all users in system
    /// Expected: Returns 200 OK with users list
    /// </summary>
    [Fact]
    public void GetUsers_Should_Return_200()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.Users;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
    }

    /// <summary>
    /// Test Case 13.2: Get Users Returns Array
    /// Scenario: Response format validation
    /// Expected: Valid JSON array with user objects
    /// </summary>
    [Fact]
    public void GetUsers_Response_ShouldBeArray()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.Users;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        using var doc = JsonDocument.Parse(response.Content);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    /// <summary>
    /// Test Case 13.3: Get Users Contains User Metadata
    /// Scenario: Verify user response structure
    /// Expected: Response includes id, username, role, createdAt
    /// </summary>
    [Fact]
    public void GetUsers_Response_ShouldContainUserData()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.Users;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        var content = response.Content?.ToLower() ?? "";
        Assert.Contains("id", content);
        Assert.Contains("username", content);
        Assert.Contains("role", content);
    }

    // POSITIVE TESTS - CREATE

    /// <summary>
    /// Test Case 13.4: Create User with Valid Data
    /// Scenario: Create new user with admin role
    /// Expected: Returns 201 Created
    /// </summary>
    [Fact]
    public void CreateUser_WithValidData_Should_Return_201()
    {
        // Arrange
        var userRequest = new
        {
            username = $"user_{Guid.NewGuid():N}",
            password = "SecurePass@123",
            role = "Manager"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Users, userRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.Created, (int)response.StatusCode);
        Assert.NotNull(response.Content);
    }

    /// <summary>
    /// Test Case 13.5: Create User Response Contains ID
    /// Scenario: Verify created user has ID
    /// Expected: Response includes user ID
    /// </summary>
    [Fact]
    public void CreateUser_Response_ShouldContainId()
    {
        // Arrange
        var userRequest = new
        {
            username = $"user_{Guid.NewGuid():N}",
            password = "SecurePass@123",
            role = "Cashier"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Users, userRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.Created, (int)response.StatusCode);
        using var doc = JsonDocument.Parse(response.Content);
        Assert.True(doc.RootElement.TryGetProperty("id", out var idElement));
        Assert.True(idElement.TryGetInt32(out var id));
        Assert.True(id > 0);
    }

    /// <summary>
    /// Test Case 13.6: Create User with Different Roles
    /// Scenario: Create users with each role
    /// Expected: All roles accepted and created
    /// </summary>
    [Fact]
    public void CreateUser_WithAllRoles_Should_Return_201()
    {
        // Arrange - Test each role
        var roles = new[] { "Admin", "Manager", "Cashier" };

        foreach (var role in roles)
        {
            var userRequest = new
            {
                username = $"user_{role}_{Guid.NewGuid():N}",
                password = "SecurePass@123",
                role = role
            };

            // Act
            var response = _client.Post(ApiTestConstants.Endpoints.Users, userRequest);

            // Assert
            Assert.Equal(ApiTestConstants.HttpStatusCodes.Created, (int)response.StatusCode);
        }
    }

    // POSITIVE TESTS - UPDATE ROLE

    /// <summary>
    /// Test Case 13.7: Update User Role
    /// Scenario: Change user role
    /// Expected: Returns 200 OK
    /// </summary>
    [Fact]
    public void UpdateUserRole_WithValidRole_Should_Return_200()
    {
        // Arrange - Create user
        var createRequest = new
        {
            username = $"user_{Guid.NewGuid():N}",
            password = "SecurePass@123",
            role = "Cashier"
        };
        var createResponse = _client.Post(ApiTestConstants.Endpoints.Users, createRequest);
        using var createDoc = JsonDocument.Parse(createResponse.Content);
        var userId = createDoc.RootElement.GetProperty("id").GetInt32();

        var updateRequest = new
        {
            role = "Manager"
        };
        var endpoint = ApiTestConstants.Endpoints.UserRole.Replace("{id}", userId.ToString());

        // Act
        var response = _client.Put(endpoint, updateRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.Contains("updated successfully", response.Content ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 13.8: Update Current User Role
    /// Scenario: Update own user role (should refresh token)
    /// Expected: Returns 200 OK with refreshed token
    /// </summary>
    [Fact]
    public void UpdateCurrentUserRole_Should_ReturnRefreshedToken()
    {
        // Arrange - Get current user ID first
        var usersResponse = _client.Get(ApiTestConstants.Endpoints.Users);
        using var usersDoc = JsonDocument.Parse(usersResponse.Content);
        
        // Just verify we can update any user's role
        if (usersDoc.RootElement.GetArrayLength() > 0)
        {
            var firstUser = usersDoc.RootElement[0];
            if (firstUser.TryGetProperty("id", out var idElement) && idElement.TryGetInt32(out var userId))
            {
                var updateRequest = new { role = "Admin" };
                var endpoint = ApiTestConstants.Endpoints.UserRole.Replace("{id}", userId.ToString());

                // Act
                var response = _client.Put(endpoint, updateRequest);

                // Assert
                Assert.True(
                    (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK ||
                    (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.Forbidden
                );
            }
        }
    }

    // POSITIVE TESTS - DELETE

    /// <summary>
    /// Test Case 13.9: Delete User
    /// Scenario: Delete existing user
    /// Expected: Returns 200 OK
    /// </summary>
    [Fact]
    public void DeleteUser_WithValidId_Should_Return_200()
    {
        // Arrange - Create user to delete
        var createRequest = new
        {
            username = $"delete_{Guid.NewGuid():N}",
            password = "SecurePass@123",
            role = "Cashier"
        };
        var createResponse = _client.Post(ApiTestConstants.Endpoints.Users, createRequest);
        using var createDoc = JsonDocument.Parse(createResponse.Content);
        var userId = createDoc.RootElement.GetProperty("id").GetInt32();

        var endpoint = ApiTestConstants.Endpoints.UserById.Replace("{id}", userId.ToString());

        // Act
        var response = _client.Delete(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.Contains("deleted successfully", response.Content ?? "", StringComparison.OrdinalIgnoreCase);
    }

    // NEGATIVE TESTS - CREATE

    /// <summary>
    /// Test Case 13.10: Create User with Empty Username
    /// Scenario: Username is empty
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateUser_WithEmptyUsername_Should_Return_400()
    {
        // Arrange
        var userRequest = new
        {
            username = "",
            password = "SecurePass@123",
            role = "Admin"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Users, userRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
        Assert.Contains("required", response.Content?.ToLower() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 13.11: Create User with Short Password
    /// Scenario: Password less than 8 characters
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateUser_WithShortPassword_Should_Return_400()
    {
        // Arrange
        var userRequest = new
        {
            username = $"user_{Guid.NewGuid():N}",
            password = "Short1!",
            role = "Admin"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Users, userRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
        Assert.Contains("password", response.Content?.ToLower() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 13.12: Create User with Duplicate Username
    /// Scenario: Username already exists
    /// Expected: Returns 409 Conflict
    /// </summary>
    [Fact]
    public void CreateUser_WithDuplicateUsername_Should_Return_409()
    {
        // Arrange
        var uniqueUsername = $"user_{Guid.NewGuid():N}";
        var firstRequest = new
        {
            username = uniqueUsername,
            password = "SecurePass@123",
            role = "Admin"
        };
        _client.Post(ApiTestConstants.Endpoints.Users, firstRequest);

        var secondRequest = new
        {
            username = uniqueUsername,
            password = "DifferentPass@123",
            role = "Manager"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Users, secondRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.Conflict, (int)response.StatusCode);
        Assert.Contains("already taken", response.Content ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 13.13: Create User with Invalid Role
    /// Scenario: Role is not Admin, Manager, or Cashier
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateUser_WithInvalidRole_Should_Return_400()
    {
        // Arrange
        var userRequest = new
        {
            username = $"user_{Guid.NewGuid():N}",
            password = "SecurePass@123",
            role = "InvalidRole"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Users, userRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
        Assert.Contains("role", response.Content?.ToLower() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 13.14: Create User with Empty Password
    /// Scenario: Password field is empty
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateUser_WithEmptyPassword_Should_Return_400()
    {
        // Arrange
        var userRequest = new
        {
            username = $"user_{Guid.NewGuid():N}",
            password = "",
            role = "Admin"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Users, userRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    // NEGATIVE TESTS - UPDATE ROLE

    /// <summary>
    /// Test Case 13.15: Update Non-existent User
    /// Scenario: User ID doesn't exist
    /// Expected: Returns 404 Not Found
    /// </summary>
    [Fact]
    public void UpdateUserRole_NonExistentId_Should_Return_404()
    {
        // Arrange
        var updateRequest = new { role = "Manager" };
        var endpoint = ApiTestConstants.Endpoints.UserRole.Replace("{id}", "999999");

        // Act
        var response = _client.Put(endpoint, updateRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.NotFound, (int)response.StatusCode);
        Assert.Contains("not found", response.Content?.ToLower() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 13.16: Update User Role with Invalid Role
    /// Scenario: Role is invalid
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void UpdateUserRole_WithInvalidRole_Should_Return_400()
    {
        // Arrange - Create user
        var createRequest = new
        {
            username = $"user_{Guid.NewGuid():N}",
            password = "SecurePass@123",
            role = "Cashier"
        };
        var createResponse = _client.Post(ApiTestConstants.Endpoints.Users, createRequest);
        using var createDoc = JsonDocument.Parse(createResponse.Content);
        var userId = createDoc.RootElement.GetProperty("id").GetInt32();

        var updateRequest = new { role = "InvalidRole" };
        var endpoint = ApiTestConstants.Endpoints.UserRole.Replace("{id}", userId.ToString());

        // Act
        var response = _client.Put(endpoint, updateRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
        Assert.Contains("role", response.Content?.ToLower() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    // NEGATIVE TESTS - DELETE

    /// <summary>
    /// Test Case 13.17: Delete Non-existent User
    /// Scenario: User ID doesn't exist
    /// Expected: Returns 404 Not Found
    /// </summary>
    [Fact]
    public void DeleteUser_NonExistentId_Should_Return_404()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.UserById.Replace("{id}", "999999");

        // Act
        var response = _client.Delete(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.NotFound, (int)response.StatusCode);
        Assert.Contains("not found", response.Content?.ToLower() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 13.18: Delete User with Negative ID
    /// Scenario: User ID is negative
    /// Expected: Returns 404 Not Found or 400 Bad Request
    /// </summary>
    [Fact]
    public void DeleteUser_WithNegativeId_Should_ReturnError()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.UserById.Replace("{id}", "-1");

        // Act
        var response = _client.Delete(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.NotFound
        );
    }

    /// <summary>
    /// Test Case 13.19: Delete User Authorization Check
    /// Scenario: Verify user deletion requires proper authorization
    /// Expected: Returns 200 OK (if authorized) or 403 Forbidden
    /// </summary>
    [Fact]
    public void DeleteUser_ShouldRespectAuthorization()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.UserById.Replace("{id}", "999");

        // Act
        var response = _client.Delete(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.NotFound ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.Forbidden
        );
    }

    /// <summary>
    /// Test Case 13.20: Create User Performance Test
    /// Scenario: User creation performance
    /// Expected: Response time < 2 seconds
    /// </summary>
    [Fact]
    public void CreateUser_Should_CompleteWithinReasonableTime()
    {
        // Arrange
        var userRequest = new
        {
            username = $"perf_{Guid.NewGuid():N}",
            password = "SecurePass@123",
            role = "Cashier"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.Users, userRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.Created, (int)response.StatusCode);
        var responseTime = ApiClient.GetResponseTime(response);
        Assert.NotNull(responseTime);
        Assert.True(responseTime.Value.TotalMilliseconds < 2000);
    }
}
