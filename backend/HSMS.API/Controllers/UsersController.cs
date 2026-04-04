using HSMS.API.Auth;
using HSMS.API.Services;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HSMS.API.Controllers;

[Route("api/users")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UsersController(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
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
