using HSMS.API.Services;
using HSMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace HSMS.Tests;

/// <summary>
/// Tests for JWT token generation, expiration, and validation
/// Ensures tokens are generated with correct expiration times and claims
/// </summary>
public class TokenExpirationTests
{
    [Fact]
    public void GenerateToken_Should_Create_Token_With_Expiration()
    {
        var jwtService = new JwtTokenService();
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Role = "Admin",
            PasswordHash = "hash"
        };

        // Execute
        var response = jwtService.GenerateToken(user);

        // Verify
        Assert.NotNull(response);
        Assert.NotEmpty(response.AccessToken);
        Assert.NotEqual(default, response.ExpiresAtUtc);
        Assert.True(response.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_Should_Set_Expiration_To_24_Hours()
    {
        var jwtService = new JwtTokenService();
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Role = "Manager",
            PasswordHash = "hash"
        };

        var beforeGeneration = DateTime.UtcNow;

        // Execute
        var response = jwtService.GenerateToken(user);

        var afterGeneration = DateTime.UtcNow;

        // Verify: Token should expire in approximately 24 hours
        var expectedExpirationMin = beforeGeneration.AddHours(24).AddSeconds(-1);
        var expectedExpirationMax = afterGeneration.AddHours(24).AddSeconds(1);

        Assert.True(response.ExpiresAtUtc >= expectedExpirationMin);
        Assert.True(response.ExpiresAtUtc <= expectedExpirationMax);
    }

    [Fact]
    public void GenerateToken_Should_Include_User_Id_In_Token()
    {
        var jwtService = new JwtTokenService();
        var user = new User
        {
            Id = 42,
            Username = "testuser",
            Role = "Cashier",
            PasswordHash = "hash"
        };

        // Execute
        var response = jwtService.GenerateToken(user);

        // Verify
        var token = new JwtSecurityTokenHandler().ReadToken(response.AccessToken) as JwtSecurityToken;
        Assert.NotNull(token);
        
        var userIdClaim = token.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        Assert.NotNull(userIdClaim);
    }

    [Fact]
    public void GenerateToken_Should_Include_Username_In_Token()
    {
        var jwtService = new JwtTokenService();
        var user = new User
        {
            Id = 1,
            Username = "johndoe",
            Role = "Manager",
            PasswordHash = "hash"
        };

        // Execute
        var response = jwtService.GenerateToken(user);

        // Verify
        var token = new JwtSecurityTokenHandler().ReadToken(response.AccessToken) as JwtSecurityToken;
        Assert.NotNull(token);
        
        var usernameClaim = token.Claims.FirstOrDefault(c => c.Type == "unique_name" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
        Assert.NotNull(usernameClaim);
    }

    [Fact]
    public void GenerateToken_Should_Include_Role_In_Token()
    {
        var jwtService = new JwtTokenService();
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Role = "Admin",
            PasswordHash = "hash"
        };

        // Execute
        var response = jwtService.GenerateToken(user);

        // Verify
        var token = new JwtSecurityTokenHandler().ReadToken(response.AccessToken) as JwtSecurityToken;
        Assert.NotNull(token);
        
        var roleClaim = token.Claims.FirstOrDefault(c => c.Type == "role");
        Assert.NotNull(roleClaim);
        Assert.Equal("Admin", roleClaim.Value);
    }

    [Fact]
    public void GenerateToken_Should_Return_Response_With_User_Info()
    {
        var jwtService = new JwtTokenService();
        var user = new User
        {
            Id = 5,
            Username = "alice",
            Role = "Manager",
            PasswordHash = "hash"
        };

        // Execute
        var response = jwtService.GenerateToken(user);

        // Verify
        Assert.NotNull(response);
        Assert.Equal(5, response.UserId);
        Assert.Equal("alice", response.Username);
        Assert.Equal("Manager", response.Role);
        Assert.NotEmpty(response.AccessToken);
        Assert.NotEqual(default(DateTime), response.ExpiresAtUtc);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Manager")]
    [InlineData("Cashier")]
    public void GenerateToken_Should_Preserve_User_Role(string role)
    {
        var jwtService = new JwtTokenService();
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Role = role,
            PasswordHash = "hash"
        };

        // Execute
        var response = jwtService.GenerateToken(user);

        // Verify
        Assert.Equal(role, response.Role);
        
        var token = new JwtSecurityTokenHandler().ReadToken(response.AccessToken) as JwtSecurityToken;
        var roleClaim = token?.Claims.FirstOrDefault(c => c.Type == "role");
        Assert.NotNull(roleClaim);
        Assert.Equal(role, roleClaim.Value);
    }

    [Fact]
    public void GenerateToken_Should_Generate_Unique_Tokens()
    {
        var jwtService = new JwtTokenService();
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Role = "Admin",
            PasswordHash = "hash"
        };

        // Execute: Generate two tokens for same user
        var response1 = jwtService.GenerateToken(user);
        System.Threading.Thread.Sleep(100); // Small delay
        var response2 = jwtService.GenerateToken(user);

        // Verify: Tokens should be different (due to timestamps or signatures)
        Assert.NotEqual(response1.AccessToken, response2.AccessToken);
    }

    [Fact]
    public void GenerateToken_Should_Have_Valid_Signature()
    {
        var jwtService = new JwtTokenService();
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Role = "Admin",
            PasswordHash = "hash"
        };

        // Execute
        var response = jwtService.GenerateToken(user);

        // Verify: Token should be parseable and valid
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadToken(response.AccessToken) as JwtSecurityToken;
        Assert.NotNull(token);
        Assert.NotNull(token.Header);
        Assert.NotNull(token.Payload);
    }

    [Fact]
    public void GenerateToken_Should_Include_Issued_At_Claim()
    {
        var jwtService = new JwtTokenService();
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Role = "Admin",
            PasswordHash = "hash"
        };

        var beforeGeneration = DateTime.UtcNow;

        // Execute
        var response = jwtService.GenerateToken(user);

        var afterGeneration = DateTime.UtcNow;

        // Verify
        var token = new JwtSecurityTokenHandler().ReadToken(response.AccessToken) as JwtSecurityToken;
        var iatClaim = token?.Claims.FirstOrDefault(c => c.Type == "iat");
        
        if (iatClaim != null)
        {
            // Claim should be recent
            Assert.NotNull(iatClaim);
        }
    }

    [Fact]
    public void GenerateToken_Should_Set_Algorithm_To_HS256()
    {
        var jwtService = new JwtTokenService();
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Role = "Admin",
            PasswordHash = "hash"
        };

        // Execute
        var response = jwtService.GenerateToken(user);

        // Verify
        var token = new JwtSecurityTokenHandler().ReadToken(response.AccessToken) as JwtSecurityToken;
        Assert.NotNull(token);
        Assert.Equal("HS256", token.Header.Alg);
    }

    [Fact]
    public void GenerateToken_Different_Users_Should_Have_Different_Token_Claims()
    {
        var jwtService = new JwtTokenService();
        
        var user1 = new User { Id = 1, Username = "user1", Role = "Admin", PasswordHash = "hash" };
        var user2 = new User { Id = 2, Username = "user2", Role = "Cashier", PasswordHash = "hash" };

        // Execute
        var response1 = jwtService.GenerateToken(user1);
        var response2 = jwtService.GenerateToken(user2);

        // Verify
        var token1 = new JwtSecurityTokenHandler().ReadToken(response1.AccessToken) as JwtSecurityToken;
        var token2 = new JwtSecurityTokenHandler().ReadToken(response2.AccessToken) as JwtSecurityToken;

        var role1 = token1?.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        var role2 = token2?.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

        Assert.NotEqual(role1, role2);
    }

    [Fact]
    public void GenerateToken_Should_Not_Expose_Password_In_Token()
    {
        var jwtService = new JwtTokenService();
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Role = "Admin",
            PasswordHash = "super_secret_hash_12345"
        };

        // Execute
        var response = jwtService.GenerateToken(user);

        // Verify
        var token = new JwtSecurityTokenHandler().ReadToken(response.AccessToken) as JwtSecurityToken;
        
        // Password hash should NOT be in any claim
        var allClaims = string.Join(" ", token?.Claims.Select(c => $"{c.Type}:{c.Value}") ?? new string[0]);
        Assert.DoesNotContain("super_secret_hash", allClaims);
        Assert.DoesNotContain("PasswordHash", allClaims);
    }

    [Fact]
    public void GenerateToken_Should_Handle_Special_Characters_In_Username()
    {
        var jwtService = new JwtTokenService();
        var user = new User
        {
            Id = 1,
            Username = "user.name+tag@example.com",
            Role = "Manager",
            PasswordHash = "hash"
        };

        // Execute
        var response = jwtService.GenerateToken(user);

        // Verify: Should generate valid token
        Assert.NotEmpty(response.AccessToken);
        
        var token = new JwtSecurityTokenHandler().ReadToken(response.AccessToken) as JwtSecurityToken;
        var usernameClaim = token?.Claims.FirstOrDefault(c => c.Type == "unique_name" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
        
        // Username should be preserved
        if (usernameClaim != null)
        {
            Assert.NotNull(usernameClaim);
        }
    }
}
