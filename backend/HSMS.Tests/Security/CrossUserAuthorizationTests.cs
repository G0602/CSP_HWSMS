using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using HSMS.API.Controllers;
using HSMS.API.Services;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using HSMS.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HSMS.Tests;

public class CrossUserAuthorizationTests
{
    private static UsersController CreateController(
        Mock<IUserRepository> userRepository,
        Mock<IPasswordHasher>? passwordHasher = null,
        Mock<IJwtTokenService>? jwtService = null,
        ClaimsPrincipal? user = null)
    {
        passwordHasher ??= new Mock<IPasswordHasher>();
        jwtService ??= new Mock<IJwtTokenService>();
        jwtService.Setup(service => service.GenerateToken(It.IsAny<User>()))
            .Returns((User u) => new AuthResponseDTO
            {
                UserId = u.Id,
                Username = u.Username,
                Role = u.Role,
                AccessToken = "refreshed-token",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(60)
            });

        var controller = new UsersController(userRepository.Object, passwordHasher.Object, jwtService.Object);

        if (user is not null)
        {
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        return controller;
    }

    [Fact]
    public async Task UpdateUserRole_Should_Refresh_Token_For_Current_User()
    {
        var userRepository = new Mock<IUserRepository>();
        var jwtService = new Mock<IJwtTokenService>();
        var existingUser = new User { Id = 1, Username = "user1", Role = "Cashier", PasswordHash = "hash" };

        userRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingUser);
        userRepository.Setup(repo => repo.UpdateRoleAsync(1, "Manager")).ReturnsAsync(true);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(JwtRegisteredClaimNames.Sub, "1") },
            "test"));

        var controller = CreateController(userRepository, jwtService: jwtService, user: principal);

        var result = await controller.UpdateUserRole(1, new UserRoleUpdateDTO { Role = "manager" });

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);

        Assert.Contains("Session token refreshed", json);
        Assert.Contains("refreshed-token", json);
        userRepository.Verify(repo => repo.UpdateRoleAsync(1, "Manager"), Times.Once);
        jwtService.Verify(service => service.GenerateToken(It.Is<User>(u => u.Id == 1 && u.Role == "Manager")), Times.Once);
    }

    [Fact]
    public async Task UpdateUserRole_Should_Return_NotFound_When_User_Does_Not_Exist()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(repo => repo.GetByIdAsync(999)).ReturnsAsync((User?)null);

        var controller = CreateController(userRepository);

        var result = await controller.UpdateUserRole(999, new UserRoleUpdateDTO { Role = "Admin" });

        Assert.IsType<NotFoundObjectResult>(result);
        userRepository.Verify(repo => repo.UpdateRoleAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("InvalidRole")]
    [InlineData("SuperAdmin")]
    public async Task UpdateUserRole_Should_Reject_Invalid_Roles(string invalidRole)
    {
        var userRepository = new Mock<IUserRepository>();
        var controller = CreateController(userRepository);

        var result = await controller.UpdateUserRole(1, new UserRoleUpdateDTO { Role = invalidRole });

        Assert.IsType<BadRequestObjectResult>(result);
        userRepository.Verify(repo => repo.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CreateUser_Should_Hash_Password_And_Normalize_Role()
    {
        var userRepository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();

        passwordHasher.Setup(hasher => hasher.HashPassword("Password@123")).Returns("hashed-password");
        userRepository.Setup(repo => repo.GetByUsernameAsync("newuser")).ReturnsAsync((User?)null);
        userRepository.Setup(repo => repo.CreateUserAsync("newuser", "hashed-password", "Admin")).ReturnsAsync(7);

        var controller = CreateController(userRepository, passwordHasher);

        var result = await controller.CreateUser(new UserCreateDTO
        {
            Username = "  newuser  ",
            Password = "Password@123",
            Role = "admin"
        });

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, created.StatusCode);
        userRepository.Verify(repo => repo.CreateUserAsync("newuser", "hashed-password", "Admin"), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_Should_Return_NotFound_When_Delete_Fails()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(repo => repo.DeleteAsync(55)).ReturnsAsync(false);

        var controller = CreateController(userRepository);

        var result = await controller.DeleteUser(55);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetUsers_Should_Not_Expose_PasswordHash()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(
        [
            new User { Id = 1, Username = "admin", Role = "Admin", PasswordHash = "secret-hash-1" },
            new User { Id = 2, Username = "cashier", Role = "Cashier", PasswordHash = "secret-hash-2" }
        ]);

        var controller = CreateController(userRepository);

        var result = await controller.GetUsers();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);

        Assert.Contains("admin", json);
        Assert.Contains("cashier", json);
        Assert.DoesNotContain("PasswordHash", json);
        Assert.DoesNotContain("secret-hash-1", json);
        Assert.DoesNotContain("secret-hash-2", json);
    }
}
