using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HSMS.API.Services;
using HSMS.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace HSMS.Tests;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService(Dictionary<string, string?>? overrides = null)
    {
        var configValues = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "hsms-test-issuer",
            ["Jwt:Audience"] = "hsms-test-audience",
            ["Jwt:Secret"] = "super-secret-key-with-enough-length-1234567890",
            ["Jwt:AccessTokenExpiryMinutes"] = "15"
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

    [Fact]
    public void GenerateToken_Should_Return_Response_With_User_Metadata()
    {
        var service = CreateService();
        var user = new User
        {
            Id = 7,
            Username = "admin_user",
            Role = "Admin",
            PasswordHash = "hash"
        };

        var result = service.GenerateToken(user);

        Assert.Equal(7, result.UserId);
        Assert.Equal("admin_user", result.Username);
        Assert.Equal("Admin", result.Role);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
    }

    [Fact]
    public void GenerateToken_Should_Include_Expected_Claims()
    {
        var service = CreateService();
        var user = new User
        {
            Id = 3,
            Username = "manager_user",
            Role = "Manager",
            PasswordHash = "hash"
        };

        var result = service.GenerateToken(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);

        Assert.Equal("hsms-test-issuer", token.Issuer);
        Assert.Contains("hsms-test-audience", token.Audiences);
        Assert.Contains(token.Claims, claim => claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == "3");
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Name && claim.Value == "manager_user");
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Manager");
    }

    [Fact]
    public void GenerateToken_Should_Fall_Back_To_Default_Expiry_When_Config_Is_Invalid()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Jwt:AccessTokenExpiryMinutes"] = "0"
        });

        var before = DateTime.UtcNow;
        var result = service.GenerateToken(new User
        {
            Id = 9,
            Username = "cashier_user",
            Role = "Cashier",
            PasswordHash = "hash"
        });
        var after = DateTime.UtcNow;

        var minimumExpected = before.AddMinutes(59);
        var maximumExpected = after.AddMinutes(61);

        Assert.InRange(result.ExpiresAtUtc, minimumExpected, maximumExpected);
    }

    [Fact]
    public void GenerateToken_Should_Throw_When_Secret_Is_Too_Short()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "short-secret"
        });

        var ex = Assert.Throws<InvalidOperationException>(() => service.GenerateToken(new User
        {
            Id = 1,
            Username = "admin",
            Role = "Admin",
            PasswordHash = "hash"
        }));

        Assert.Contains("at least 32 bytes", ex.Message);
    }
}
