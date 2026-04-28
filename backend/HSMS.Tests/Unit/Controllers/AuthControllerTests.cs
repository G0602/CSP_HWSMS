using HSMS.API.Controllers;
using HSMS.API.Services;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
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
        authenticationService ??= new Mock<IAuthenticationService>();
        jwtTokenService ??= new Mock<IJwtTokenService>();

        return new AuthController(
            userRepository.Object,
            passwordHasher.Object,
            jwtTokenService.Object,
            authenticationService.Object);
    }

    [Fact]
    public void Register_Should_Return_Forbidden_When_SelfRegistration_Is_Used()
    {
        var controller = CreateController(new Mock<IUserRepository>());

        var result = controller.Register(new AuthRegisterDTO
        {
            Username = "new-user",
            Password = "Password@123",
            Role = "Cashier"
        });

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbidden.StatusCode);
        Assert.Equal("Self-registration is disabled. Contact an administrator to create a new user.", forbidden.Value);
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
