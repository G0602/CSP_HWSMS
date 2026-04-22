using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HSMS.Application.DTOs;
using HSMS.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace HSMS.API.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public AuthResponseDTO GenerateToken(User user)
    {
        string issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is missing");
        string audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is missing");
        string secret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is missing");

        if (Encoding.UTF8.GetByteCount(secret) < 32)
        {
            throw new InvalidOperationException("Jwt:Secret must be at least 32 bytes for secure signing.");
        }

        int expiryMinutes = 60;
        _ = int.TryParse(_configuration["Jwt:AccessTokenExpiryMinutes"], out expiryMinutes);
        if (expiryMinutes <= 0)
        {
            expiryMinutes = 60;
        }

        DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new AuthResponseDTO
        {
            UserId = user.Id,
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expiresAtUtc,
            Username = user.Username,
            Role = user.Role
        };
    }
}
