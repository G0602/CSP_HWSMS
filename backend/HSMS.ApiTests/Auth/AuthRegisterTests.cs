using Xunit;
using HSMS.ApiTests.Helpers;

namespace HSMS.ApiTests;

/// <summary>
/// Self-registration is intentionally disabled.
/// New users must be created by an existing admin via the user-management API.
/// </summary>
public class AuthRegisterTests
{
    private readonly ApiClient _client = new ApiClient();

    [Fact]
    public void Register_WithValidData_Should_Return_403()
    {
        var registerRequest = new
        {
            username = $"adminuser_{Guid.NewGuid():N}",
            password = "SecurePass@123",
            role = "Admin"
        };

        var response = _client.Post(ApiTestConstants.Endpoints.AuthRegister, registerRequest);

        Assert.Equal(ApiTestConstants.HttpStatusCodes.Forbidden, (int)response.StatusCode);
        Assert.Contains("disabled", response.Content ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Register_WithoutRole_Should_Still_Return_403()
    {
        var registerRequest = new
        {
            username = $"defaultuser_{Guid.NewGuid():N}",
            password = "SecurePass@123"
        };

        var response = _client.Post(ApiTestConstants.Endpoints.AuthRegister, registerRequest);

        Assert.Equal(ApiTestConstants.HttpStatusCodes.Forbidden, (int)response.StatusCode);
    }

    [Fact]
    public void Register_WithEmptyUsername_Should_Return_403()
    {
        var registerRequest = new
        {
            username = "",
            password = "SecurePass@123",
            role = "Admin"
        };

        var response = _client.Post(ApiTestConstants.Endpoints.AuthRegister, registerRequest);

        Assert.Equal(ApiTestConstants.HttpStatusCodes.Forbidden, (int)response.StatusCode);
    }

    [Fact]
    public void Register_WithDuplicateUsername_Should_Return_403()
    {
        var registerRequest = new
        {
            username = $"duplicate_{Guid.NewGuid():N}",
            password = "DifferentPass@123",
            role = "Manager"
        };

        var response = _client.Post(ApiTestConstants.Endpoints.AuthRegister, registerRequest);

        Assert.Equal(ApiTestConstants.HttpStatusCodes.Forbidden, (int)response.StatusCode);
    }
}
