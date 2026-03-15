using HSMS.API.Services;
using HSMS.API.Auth;
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
    private readonly IAuthenticationService _authenticationService;

    public AuthController(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IAuthenticationService authenticationService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _authenticationService = authenticationService;
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

        string role = string.IsNullOrWhiteSpace(dto.Role) ? AppRoles.Cashier : dto.Role.Trim();
        if (!IsAllowedRole(role))
        {
            return BadRequest("Role must be one of 'Admin', 'Manager', 'Cashier', or 'User'.");
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
        try
        {
            AuthResponseDTO auth = await _authenticationService.LoginAsync(dto);
            return Ok(auth);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    private static bool IsAllowedRole(string role)
    {
        return role.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase)
            || role.Equals(AppRoles.Manager, StringComparison.OrdinalIgnoreCase)
            || role.Equals(AppRoles.Cashier, StringComparison.OrdinalIgnoreCase)
            || role.Equals(AppRoles.User, StringComparison.OrdinalIgnoreCase);
    }
}
