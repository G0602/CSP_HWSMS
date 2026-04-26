using Xunit;
using HSMS.ApiTests.Helpers;

namespace HSMS.ApiTests;

/// <summary>
/// MEMBER 1 - Authentication API Tests (Positive Scenarios)
/// 
/// Tests valid authentication workflows:
/// - Successful login with correct credentials
/// - Successful user registration
/// - Token response validation
/// - User metadata in response
/// 
/// Focus: Functional correctness, happy path scenarios
/// </summary>
public class AuthApiTests
{
    private readonly ApiClient _client = new ApiClient();

    /// <summary>
    /// Test Case 1.1: Valid Login
    /// Scenario: User logs in with correct username and password
    /// Expected: Returns 200 OK with access token
    /// </summary>
    [Fact]
    public void Login_WithValidCredentials_Should_Return_200_And_AccessToken()
    {
        // Arrange
        var loginRequest = new
        {
            username = ApiTestConstants.TestAdminUsername,
            password = ApiTestConstants.TestAdminPassword
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthLogin, loginRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
        Assert.Contains("accessToken", response.Content);
        Assert.Contains("userId", response.Content);
        Assert.Contains("username", response.Content);
        Assert.Contains("role", response.Content);
    }

    /// <summary>
    /// Test Case 1.2: Login Response Contains Valid Token
    /// Scenario: Verify the token is not empty and has expected format
    /// Expected: Token is a non-empty JWT-like string
    /// </summary>
    [Fact]
    public void Login_Response_Should_ContainValidToken()
    {
        // Arrange
        var loginRequest = new
        {
            username = ApiTestConstants.TestAdminUsername,
            password = ApiTestConstants.TestAdminPassword
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthLogin, loginRequest);
        var contentLower = response.Content?.ToLower() ?? "";

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.True(response.Content?.Length > 50, "Token response should contain substantial data");
        Assert.Contains("accesstoken", contentLower);
    }

    /// <summary>
    /// Test Case 1.3: Login Response Contains User ID
    /// Scenario: Verify response includes user metadata
    /// Expected: Response contains user ID and username
    /// </summary>
    [Fact]
    public void Login_Response_Should_ContainUserId()
    {
        // Arrange
        var loginRequest = new
        {
            username = ApiTestConstants.TestAdminUsername,
            password = ApiTestConstants.TestAdminPassword
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthLogin, loginRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.Contains("\"userId\"", response.Content ?? "");
        Assert.Contains(ApiTestConstants.TestAdminUsername, response.Content ?? "");
    }

    /// <summary>
    /// Test Case 1.4: Login Response Contains Role Information
    /// Scenario: Verify user role is returned in response
    /// Expected: Response includes role (Admin/Manager/Cashier)
    /// </summary>
    [Fact]
    public void Login_Response_Should_ContainUserRole()
    {
        // Arrange
        var loginRequest = new
        {
            username = ApiTestConstants.TestAdminUsername,
            password = ApiTestConstants.TestAdminPassword
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthLogin, loginRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.Contains("role", response.Content?.ToLower() ?? "");
        // Admin user should have Admin role
        Assert.Contains("Admin", response.Content ?? "");
    }

    /// <summary>
    /// Test Case 1.5: Login with Different Valid User (Manager)
    /// Scenario: Verify login works for different roles
    /// Expected: Returns 200 OK with appropriate role
    /// </summary>
    [Fact]
    public void Login_WithValidManagerCredentials_Should_Return_200()
    {
        // Arrange
        var loginRequest = new
        {
            username = ApiTestConstants.TestManagerUsername,
            password = ApiTestConstants.TestManagerPassword
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthLogin, loginRequest);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.Contains("accessToken", response.Content ?? "");
        Assert.Contains("Manager", response.Content ?? "");
    }

    /// <summary>
    /// Test Case 1.6: Response Time Performance
    /// Scenario: Login should respond quickly
    /// Expected: Response time < 2 seconds
    /// </summary>
    [Fact]
    public void Login_Should_RespondWithinAcceptableTime()
    {
        // Arrange
        var loginRequest = new
        {
            username = ApiTestConstants.TestAdminUsername,
            password = ApiTestConstants.TestAdminPassword
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthLogin, loginRequest);
        var responseTime = ApiClient.GetResponseTime(response);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.True(responseTime?.TotalSeconds < 2, 
            $"Response took {responseTime?.TotalSeconds} seconds, expected < 2 seconds");
    }

    /// <summary>
    /// Test Case 1.7: Login Endpoint Availability
    /// Scenario: Verify the endpoint is accessible
    /// Expected: Does not return 404 or 500
    /// </summary>
    [Fact]
    public void AuthLogin_Endpoint_Should_BeAccessible()
    {
        // Arrange
        var loginRequest = new
        {
            username = ApiTestConstants.TestAdminUsername,
            password = ApiTestConstants.TestAdminPassword
        };

        // Act
        var response = _client.Post(ApiTestConstants.Endpoints.AuthLogin, loginRequest);

        // Assert
        Assert.NotEqual(ApiTestConstants.HttpStatusCodes.NotFound, (int)response.StatusCode);
        Assert.NotEqual(ApiTestConstants.HttpStatusCodes.InternalServerError, (int)response.StatusCode);
    }
}
