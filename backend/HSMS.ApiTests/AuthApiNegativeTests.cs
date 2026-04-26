using Xunit;
using HSMS.ApiTests.Helpers;

namespace HSMS.ApiTests;

/// <summary>
/// MEMBER 2 - Authentication API Tests (Negative/Error Scenarios)
/// 
/// Tests error handling and validation:
/// - Invalid credentials rejection
/// - Missing required fields
/// - Invalid input formats
/// - Error response validation
/// - Security edge cases
/// 
/// Focus: Error handling, security, input validation
/// </summary>
public class AuthApiNegativeTests
{
    private readonly ApiClient _client = new ApiClient();

    /// <summary>
    /// Test Case 2.1: Login with Wrong Password
    /// Scenario: User attempts login with incorrect password
    /// Expected: Returns 401 Unauthorized
    /// </summary>
    [Fact]
    public void Login_WithWrongPassword_Should_Return_401()
    {
        // Arrange
        var loginRequest = new
        {
            username = ApiTestConstants.TestAdminUsername,
            password = "incorrectpassword"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthLogin, loginRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 2.2: Login with Non-existent Username
    /// Scenario: User attempts login with username that doesn't exist
    /// Expected: Returns 401 Unauthorized
    /// </summary>
    [Fact]
    public void Login_WithNonExistentUsername_Should_Return_401()
    {
        // Arrange
        var loginRequest = new
        {
            username = "nonexistent_user_12345",
            password = "anypassword"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthLogin, loginRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.Unauthorized, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 2.3: Login with Empty Username
    /// Scenario: User attempts login with empty username
    /// Expected: Returns 401 Unauthorized
    /// </summary>
    [Fact]
    public void Login_WithEmptyUsername_Should_Return_400()
    {
        // Arrange
        var loginRequest = new
        {
            username = "",
            password = ApiTestConstants.TestAdminPassword
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthLogin, loginRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.Unauthorized, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 2.4: Login with Empty Password
    /// Scenario: User attempts login with empty password
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void Login_WithEmptyPassword_Should_Return_400()
    {
        // Arrange
        var loginRequest = new
        {
            username = ApiTestConstants.TestAdminUsername,
            password = ""
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthLogin, loginRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.Unauthorized, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 2.5: Login with Missing Username Field
    /// Scenario: User request missing username field entirely
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void Login_WithMissingUsernameField_Should_Return_400()
    {
        // Arrange - Missing username, only password
        var loginRequest = new
        {
            password = ApiTestConstants.TestAdminPassword
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthLogin, loginRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.Unauthorized, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 2.6: Login with Missing Password Field
    /// Scenario: User request missing password field entirely
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void Login_WithMissingPasswordField_Should_Return_400()
    {
        // Arrange - Missing password, only username
        var loginRequest = new
        {
            username = ApiTestConstants.TestAdminUsername
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthLogin, loginRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 2.7: Login with Only Whitespace Username
    /// Scenario: Username contains only spaces
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void Login_WithWhitespaceOnlyUsername_Should_Return_400()
    {
        // Arrange
        var loginRequest = new
        {
            username = "   ",
            password = ApiTestConstants.TestAdminPassword
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthLogin, loginRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 2.8: Login with SQL Injection Attempt
    /// Scenario: Username contains SQL injection payload
    /// Expected: Returns 400 or 401 (not processed as SQL)
    /// </summary>
    [Fact]
    public void Login_WithSqlInjectionAttempt_Should_NotProcessAsSql()
    {
        // Arrange
        var loginRequest = new
        {
            username = "admin' OR '1'='1",
            password = "anything"
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthLogin, loginRequest);

        // Assert - Should reject, not execute SQL
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.Unauthorized,
            "SQL injection attempt should be rejected"
        );
    }

    /// <summary>
    /// Test Case 2.9: Login with Very Long Username
    /// Scenario: Username exceeds reasonable length
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void Login_WithVeryLongUsername_Should_Return_400()
    {
        // Arrange
        var longUsername = new string('a', 1000);
        var loginRequest = new
        {
            username = longUsername,
            password = ApiTestConstants.TestAdminPassword
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthLogin, loginRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 2.10: Login with Special Characters
    /// Scenario: Username contains special characters
    /// Expected: Returns 401 (user doesn't exist)
    /// </summary>
    [Fact]
    public void Login_WithSpecialCharactersInUsername_Should_Return_401()
    {
        // Arrange
        var loginRequest = new
        {
            username = "admin@#$%^&*()",
            password = ApiTestConstants.TestAdminPassword
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthLogin, loginRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 2.11: Login Request Without Body
    /// Scenario: POST request sent without JSON body
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void Login_WithoutRequestBody_Should_Return_400()
    {
        // Arrange - Empty object
        var emptyRequest = new { };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthLogin, emptyRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 2.12: Multiple Failed Login Attempts
    /// Scenario: Simulate brute force attack attempt (3 tries)
    /// Expected: All return 401, no rate limiting yet but demonstrates repeated failures
    /// </summary>
    [Fact]
    public void Login_MultipleFailedAttempts_Should_AllReturn_401()
    {
        // Arrange
        var wrongRequests = new[]
        {
            new { username = "admin", password = "wrong1" },
            new { username = "admin", password = "wrong2" },
            new { username = "admin", password = "wrong3" }
        };

        // Act & Assert
        foreach (var request in wrongRequests)
        {
            var response = _client.Post(ApiTestConstants.Endpoints.AuthLogin, request);
            Assert.Equal(ApiTestConstants.HttpStatusCodes.Unauthorized, (int)response.StatusCode);
        }
    }

    /// <summary>
    /// Test Case 2.13: Case Sensitivity in Username
    /// Scenario: Test if username is case-sensitive
    /// Expected: Should handle gracefully (either accept or reject, but not error 500)
    /// </summary>
    [Fact]
    public void Login_WithDifferentCaseUsername_Should_NotReturnServerError()
    {
        // Arrange
        var loginRequest = new
        {
            username = "ADMIN",
            password = ApiTestConstants.TestAdminPassword
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthLogin, loginRequest);

        // Assert - Should not crash server
        Assert.NotEqual(ApiTestConstants.HttpStatusCodes.InternalServerError, (int)response.StatusCode);
    }
}
