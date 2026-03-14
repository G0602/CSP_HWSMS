using HSMS.API.Services;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using HSMS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HSMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(AuthRegisterDTO dto)
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

        string role = string.IsNullOrWhiteSpace(dto.Role) ? "User" : dto.Role.Trim();
        if (!IsAllowedRole(role))
        {
            return BadRequest("Role must be either 'Admin' or 'User'.");
        }

        var existingUser = await _userRepository.GetByUsernameAsync(username);
        if (existingUser is not null)
        {
            return Conflict("Username is already taken.");
        }

        string passwordHash = _passwordHasher.HashPassword(dto.Password);
        int userId = await _userRepository.CreateUserAsync(username, passwordHash, role);

        var user = new User
        {
            Id = userId,
            Username = username,
            PasswordHash = passwordHash,
            Role = role
        };

        AuthResponseDTO auth = _jwtTokenService.GenerateToken(user);
        return Ok(auth);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(AuthLoginDTO dto)
    {
        string username = dto.Username.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest("Username and password are required.");
        }

        var user = await _userRepository.GetByUsernameAsync(username);
        if (user is null || !_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            return Unauthorized("Invalid username or password.");
        }

        AuthResponseDTO auth = _jwtTokenService.GenerateToken(user);
        return Ok(auth);
    }

    private static bool IsAllowedRole(string role)
    {
        return role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            || role.Equals("User", StringComparison.OrdinalIgnoreCase);
    }
}
