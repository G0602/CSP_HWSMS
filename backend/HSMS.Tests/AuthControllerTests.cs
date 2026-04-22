using HSMS.API.Controllers;
using HSMS.API.Services;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using HSMS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HSMS.Tests;

public class AuthControllerTests
{
    private static AuthController CreateController(
        Mock<IUserRepository> userRepository,
        Mock<IPasswordHasher>? passwordHasher = null,
        Mock<IJwtTokenService>? jwtTokenService = null,
        Mock<IAuthenticationService>? authenticationService = null)
    {
        passwordHasher ??= new Mock<IPasswordHasher>();
        jwtTokenService ??= new Mock<IJwtTokenService>();
        authenticationService ??= new Mock<IAuthenticationService>();

        jwtTokenService.Setup(service => service.GenerateToken(It.IsAny<User>()))
            .Returns((User user) => new AuthResponseDTO
            {
                UserId = user.Id,
                Username = user.Username,
                Role = user.Role,
                AccessToken = "generated-token",
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
            });

        passwordHasher.Setup(service => service.HashPassword(It.IsAny<string>()))
            .Returns((string password) => $"hashed::{password}");

        return new AuthController(
            userRepository.Object,
            passwordHasher.Object,
            jwtTokenService.Object,
            authenticationService.Object);
    }

    [Fact]
    public async Task Register_Should_Return_BadRequest_When_Username_Is_Blank()
    {
        var controller = CreateController(new Mock<IUserRepository>());

        var result = await controller.Register(new AuthRegisterDTO
        {
            Username = "   ",
            Password = "Password@123",
            Role = "Admin"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task Register_Should_Default_Empty_Role_To_Cashier()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(repo => repo.GetByUsernameAsync("new-user")).ReturnsAsync((User?)null);
        userRepository.Setup(repo => repo.CreateUserAsync("new-user", It.IsAny<string>(), "Cashier")).ReturnsAsync(10);

        var controller = CreateController(userRepository);

        var result = await controller.Register(new AuthRegisterDTO
        {
            Username = "new-user",
            Password = "Password@123",
            Role = ""
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        userRepository.Verify(repo => repo.CreateUserAsync("new-user", It.IsAny<string>(), "Cashier"), Times.Once);
    }

    [Fact]
    public async Task Register_Should_Return_Conflict_When_Username_Exists()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(repo => repo.GetByUsernameAsync("existing"))
            .ReturnsAsync(new User { Id = 1, Username = "existing", Role = "Cashier", PasswordHash = "hash" });

        var controller = CreateController(userRepository);

        var result = await controller.Register(new AuthRegisterDTO
        {
            Username = "existing",
            Password = "Password@123",
            Role = "Manager"
        });

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task Login_Should_Return_Ok_When_Credentials_Are_Valid()
    {
        var userRepository = new Mock<IUserRepository>();
        var authenticationService = new Mock<IAuthenticationService>();
        authenticationService.Setup(service => service.LoginAsync(It.IsAny<AuthLoginDTO>()))
            .ReturnsAsync(new AuthResponseDTO
            {
                UserId = 5,
                Username = "manager",
                Role = "Manager",
                AccessToken = "valid-token",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
            });

        var controller = CreateController(userRepository, authenticationService: authenticationService);

        var result = await controller.Login(new AuthLoginDTO
        {
            Username = "manager",
            Password = "Password@123"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AuthResponseDTO>(ok.Value);
        Assert.Equal("manager", payload.Username);
        Assert.Equal("Manager", payload.Role);
    }

    [Fact]
    public async Task Login_Should_Return_BadRequest_When_Service_Rejects_Input()
    {
        var userRepository = new Mock<IUserRepository>();
        var authenticationService = new Mock<IAuthenticationService>();
        authenticationService.Setup(service => service.LoginAsync(It.IsAny<AuthLoginDTO>()))
            .ThrowsAsync(new InvalidOperationException("Username and password are required."));

        var controller = CreateController(userRepository, authenticationService: authenticationService);

        var result = await controller.Login(new AuthLoginDTO());

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.Equal("Username and password are required.", badRequest.Value);
    }

    [Fact]
    public async Task Login_Should_Return_Unauthorized_When_Credentials_Are_Invalid()
    {
        var userRepository = new Mock<IUserRepository>();
        var authenticationService = new Mock<IAuthenticationService>();
        authenticationService.Setup(service => service.LoginAsync(It.IsAny<AuthLoginDTO>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid username or password."));

        var controller = CreateController(userRepository, authenticationService: authenticationService);

        var result = await controller.Login(new AuthLoginDTO
        {
            Username = "cashier",
            Password = "wrong"
        });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorized.StatusCode);
        Assert.Equal("Invalid username or password.", unauthorized.Value);
    }
}
