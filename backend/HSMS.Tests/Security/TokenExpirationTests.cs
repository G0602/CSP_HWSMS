using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HSMS.API.Services;
using HSMS.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace HSMS.Tests;

public class TokenExpirationTests
{
    private static JwtTokenService CreateService(Dictionary<string, string?>? overrides = null)
    {
        var configValues = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "hsms-test-issuer",
            ["Jwt:Audience"] = "hsms-test-audience",
            ["Jwt:Secret"] = "super-secret-key-with-enough-length-1234567890",
            ["Jwt:AccessTokenExpiryMinutes"] = "60"
        };

        if (overrides is not null)
        {
            foreach (var entry in overrides)
            {
                configValues[entry.Key] = entry.Value;
            }
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        return new JwtTokenService(configuration);
    }

    private static User CreateUser(int id = 1, string username = "testuser", string role = "Admin")
    {
        return new User
        {
            Id = id,
            Username = username,
            Role = role,
            PasswordHash = "hash"
        };
    }

    [Fact]
    public void GenerateToken_Should_Create_Token_With_Future_Expiration()
    {
        var service = CreateService();

        var response = service.GenerateToken(CreateUser());

        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.True(response.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_Should_Set_Expiration_Based_On_Configured_Minutes()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Jwt:AccessTokenExpiryMinutes"] = "90"
        });

        var before = DateTime.UtcNow;
        var response = service.GenerateToken(CreateUser());
        var after = DateTime.UtcNow;

        Assert.InRange(response.ExpiresAtUtc, before.AddMinutes(89), after.AddMinutes(91));
    }

    [Fact]
    public void GenerateToken_Should_Fall_Back_To_60_Minutes_When_Config_Is_Invalid()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Jwt:AccessTokenExpiryMinutes"] = "0"
        });

        var before = DateTime.UtcNow;
        var response = service.GenerateToken(CreateUser());
        var after = DateTime.UtcNow;

        Assert.InRange(response.ExpiresAtUtc, before.AddMinutes(59), after.AddMinutes(61));
    }

    [Fact]
    public void GenerateToken_Should_Include_Sub_Name_And_Role_Claims()
    {
        var service = CreateService();

        var response = service.GenerateToken(CreateUser(id: 42, username: "johndoe", role: "Manager"));
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);

        Assert.Contains(token.Claims, claim => claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == "42");
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Name && claim.Value == "johndoe");
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Manager");
        Assert.Contains(token.Claims, claim => claim.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateToken_Should_Return_Response_With_User_Metadata()
    {
        var service = CreateService();

        var response = service.GenerateToken(CreateUser(id: 5, username: "alice", role: "Cashier"));

        Assert.Equal(5, response.UserId);
        Assert.Equal("alice", response.Username);
        Assert.Equal("Cashier", response.Role);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
    }

    [Fact]
    public void GenerateToken_Should_Generate_Unique_Tokens_For_The_Same_User()
    {
        var service = CreateService();
        var user = CreateUser();

        var first = service.GenerateToken(user);
        var second = service.GenerateToken(user);

        Assert.NotEqual(first.AccessToken, second.AccessToken);
    }
}
