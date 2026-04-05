using HSMS.API.Controllers;
using HSMS.API.Services;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using HSMS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Moq;

namespace HSMS.Tests;

public class UsersControllerTests
{
    private static UsersController CreateController(
        Mock<IUserRepository> userRepo,
        Mock<IJwtTokenService>? jwtService = null)
    {
        jwtService ??= new Mock<IJwtTokenService>();
        jwtService.Setup(service => service.GenerateToken(It.IsAny<User>()))
            .Returns(new AuthResponseDTO
            {
                UserId = 1,
                AccessToken = "token",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(60),
                Username = "admin",
                Role = "Admin"
            });

        return new UsersController(userRepo.Object, new PasswordHasher(), jwtService.Object);
    }

    [Fact]
    public async Task CreateUser_Should_Return_Created_When_Valid()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(repo => repo.GetByUsernameAsync("new-admin"))
            .ReturnsAsync((User?)null);
        userRepo.Setup(repo => repo.GetAllAsync()).ReturnsAsync([]);
        userRepo.Setup(repo => repo.CreateUserAsync(
                "new-admin",
                It.IsAny<string>(),
                "Admin"))
            .ReturnsAsync(101);

        var controller = CreateController(userRepo);

        var dto = new UserCreateDTO
        {
            Username = "new-admin",
            Password = "Password@123",
            Role = "admin"
        };

        var result = await controller.CreateUser(dto);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, created.StatusCode);

        userRepo.Verify(repo => repo.CreateUserAsync(
            "new-admin",
            It.Is<string>(hash => hash != dto.Password && hash.Count(c => c == '.') == 2),
            "Admin"), Times.Once);
    }

    [Fact]
    public async Task CreateUser_Should_Return_BadRequest_When_Role_Invalid()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(repo => repo.GetAllAsync()).ReturnsAsync([]);
        var controller = CreateController(userRepo);

        var result = await controller.CreateUser(new UserCreateDTO
        {
            Username = "test-user",
            Password = "Password@123",
            Role = "Supervisor"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task CreateUser_Should_Return_Conflict_When_Username_Exists()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(repo => repo.GetByUsernameAsync("existing-user"))
            .ReturnsAsync(new User
            {
                Id = 1,
                Username = "existing-user",
                PasswordHash = "hash",
                Role = "Cashier"
            });

        var controller = CreateController(userRepo);

        var result = await controller.CreateUser(new UserCreateDTO
        {
            Username = "existing-user",
            Password = "Password@123",
            Role = "Cashier"
        });

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task CreateUser_Should_Default_Role_To_Cashier_When_Empty()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(repo => repo.GetByUsernameAsync("new-user"))
            .ReturnsAsync((User?)null);
        userRepo.Setup(repo => repo.GetAllAsync()).ReturnsAsync([]);
        userRepo.Setup(repo => repo.CreateUserAsync(
                "new-user",
                It.IsAny<string>(),
                "Cashier"))
            .ReturnsAsync(102);

        var controller = CreateController(userRepo);

        var result = await controller.CreateUser(new UserCreateDTO
        {
            Username = "new-user",
            Password = "Password@123",
            Role = ""
        });

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, created.StatusCode);
    }

    [Fact]
    public async Task GetUsers_Should_Return_Ok_With_Sanitized_Users()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(repo => repo.GetAllAsync()).ReturnsAsync(
        [
            new User
            {
                Id = 1,
                Username = "admin",
                PasswordHash = "hash1",
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = 2,
                Username = "cashier",
                PasswordHash = "hash2",
                Role = "Cashier",
                CreatedAt = DateTime.UtcNow
            }
        ]);

        var controller = CreateController(userRepo);
        var result = await controller.GetUsers();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task UpdateUserRole_Should_Return_BadRequest_When_Role_Invalid()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(repo => repo.GetAllAsync()).ReturnsAsync([]);
        var controller = CreateController(userRepo);

        var result = await controller.UpdateUserRole(10, new UserRoleUpdateDTO { Role = "Supervisor" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task UpdateUserRole_Should_Return_NotFound_When_User_Does_Not_Exist()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(repo => repo.GetAllAsync()).ReturnsAsync([]);
        userRepo.Setup(repo => repo.GetByIdAsync(10)).ReturnsAsync((User?)null);

        var controller = CreateController(userRepo);

        var result = await controller.UpdateUserRole(10, new UserRoleUpdateDTO { Role = "Manager" });

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public async Task UpdateUserRole_Should_Return_Ok_When_Another_User_Updated()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(repo => repo.GetAllAsync()).ReturnsAsync([]);
        userRepo.Setup(repo => repo.GetByIdAsync(2)).ReturnsAsync(new User
        {
            Id = 2,
            Username = "cashier",
            PasswordHash = "hash",
            Role = "Cashier"
        });
        userRepo.Setup(repo => repo.UpdateRoleAsync(2, "Manager")).ReturnsAsync(true);

        var controller = CreateController(userRepo);

        var result = await controller.UpdateUserRole(2, new UserRoleUpdateDTO { Role = "Manager" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task UpdateUserRole_Should_Return_Refreshed_Token_When_Current_User_Updated()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(repo => repo.GetAllAsync()).ReturnsAsync([]);
        userRepo.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = "hash",
            Role = "Admin"
        });
        userRepo.Setup(repo => repo.UpdateRoleAsync(1, "Manager")).ReturnsAsync(true);

        var jwtService = new Mock<IJwtTokenService>();
        jwtService.Setup(service => service.GenerateToken(It.Is<User>(u => u.Id == 1 && u.Role == "Manager")))
            .Returns(new AuthResponseDTO
            {
                UserId = 1,
                AccessToken = "new-token",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(60),
                Username = "admin",
                Role = "Manager"
            });

        var controller = CreateController(userRepo, jwtService);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim("sub", "1"),
                    new Claim(ClaimTypes.Role, "Admin")
                ], "TestAuth"))
            }
        };

        var result = await controller.UpdateUserRole(1, new UserRoleUpdateDTO { Role = "Manager" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        jwtService.Verify(service => service.GenerateToken(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_Should_Return_NotFound_When_User_Does_Not_Exist()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(repo => repo.GetAllAsync()).ReturnsAsync([]);
        userRepo.Setup(repo => repo.DeleteAsync(99)).ReturnsAsync(false);
        var controller = CreateController(userRepo);

        var result = await controller.DeleteUser(99);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_Should_Return_Ok_When_Deleted()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(repo => repo.GetAllAsync()).ReturnsAsync([]);
        userRepo.Setup(repo => repo.DeleteAsync(2)).ReturnsAsync(true);
        var controller = CreateController(userRepo);

        var result = await controller.DeleteUser(2);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }
}
