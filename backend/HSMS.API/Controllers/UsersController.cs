using HSMS.API.Auth;
using HSMS.API.Services;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace HSMS.API.Controllers;

[Route("api/users")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public UsersController(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    [Authorize(Policy = AuthPolicies.UsersManage)]
    [HttpPost]
    public async Task<IActionResult> CreateUser(UserCreateDTO dto)
    {
        string username = dto.Username.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest("Username is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
        {
            return BadRequest("Password must be at least 8 characters long.");
        }

        string role = NormalizeRole(dto.Role);
        if (string.IsNullOrWhiteSpace(role))
        {
            return BadRequest("Role must be one of 'Admin', 'Manager', or 'Cashier'.");
        }

        var existingUser = await _userRepository.GetByUsernameAsync(username);
        if (existingUser is not null)
        {
            return Conflict("Username is already taken.");
        }

        string passwordHash = _passwordHasher.HashPassword(dto.Password);
        int userId = await _userRepository.CreateUserAsync(username, passwordHash, role);

        return Created($"/api/users/{userId}", new
        {
            id = userId,
            username,
            role
        });
    }

    [Authorize(Policy = AuthPolicies.UsersManage)]
    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> UpdateUserRole(int id, UserRoleUpdateDTO dto)
    {
        string role = NormalizeRole(dto.Role);
        if (string.IsNullOrWhiteSpace(role))
        {
            return BadRequest("Role must be one of 'Admin', 'Manager', or 'Cashier'.");
        }

        var existingUser = await _userRepository.GetByIdAsync(id);
        if (existingUser is null)
        {
            return NotFound("User not found.");
        }

        bool updated = await _userRepository.UpdateRoleAsync(id, role);
        if (!updated)
        {
            return NotFound("User not found.");
        }

        existingUser.Role = role;

        int? currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue && currentUserId.Value == id)
        {
            var refreshedAuth = _jwtTokenService.GenerateToken(existingUser);
            return Ok(new
            {
                message = "User role updated successfully. Session token refreshed.",
                auth = refreshedAuth
            });
        }

        return Ok("User role updated successfully.");
    }

    private int? GetCurrentUserId()
    {
        string? subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (int.TryParse(subject, out int userId))
        {
            return userId;
        }

        return null;
    }

    private static string NormalizeRole(string role)
    {
        string value = string.IsNullOrWhiteSpace(role) ? AppRoles.Cashier : role.Trim();

        if (value.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
            return AppRoles.Admin;
        if (value.Equals(AppRoles.Manager, StringComparison.OrdinalIgnoreCase))
            return AppRoles.Manager;
        if (value.Equals(AppRoles.Cashier, StringComparison.OrdinalIgnoreCase))
            return AppRoles.Cashier;

        return string.Empty;
    }
}
