using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;

namespace HSMS.API.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthenticationService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDTO> LoginAsync(AuthLoginDTO dto)
    {
        string username = dto.Username.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(dto.Password))
        {
            throw new InvalidOperationException("Username and password are required.");
        }

        var user = await _userRepository.GetByUsernameAsync(username);
        if (user is null || !_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        return _jwtTokenService.GenerateToken(user);
    }
}
