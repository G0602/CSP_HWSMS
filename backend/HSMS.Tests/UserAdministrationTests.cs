using Xunit;
using Moq;
using HSMS.API.Controllers;
using HSMS.API.Services;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using HSMS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HSMS.Tests;

/// <summary>
/// Sprint 3 - EPIC 3.3: User & Role Administration Unit Tests
/// Tests for stories: S3-US-07, S3-US-08, S3-US-09
/// </summary>
public class UserAdministrationTests
{
    private static UsersController CreateUsersController(
        Mock<IUserRepository> userRepo,
        Mock<IJwtTokenService>? jwtService = null,
        Mock<IPasswordHasher>? passwordHasher = null)
    {
        jwtService ??= new Mock<IJwtTokenService>();
        jwtService.Setup(s => s.GenerateToken(It.IsAny<User>()))
            .Returns(new AuthResponseDTO
            {
                UserId = 1,
                AccessToken = "test-token",
                ExpiresAtUtc = System.DateTime.UtcNow.AddHours(1),
                Username = "testuser",
                Role = "Admin"
            });

        passwordHasher ??= new Mock<IPasswordHasher>();
        passwordHasher.Setup(p => p.HashPassword(It.IsAny<string>()))
            .Returns((string pwd) => $"hashed_{pwd}");

        return new UsersController(userRepo.Object, passwordHasher.Object, jwtService.Object);
    }

    #region Story S3-US-07: Admin Creates Users

    [Fact]
    public async Task CreateUser_Should_Return_Created_When_Valid()
    {
        // Arrange
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByUsernameAsync("newadmin")).ReturnsAsync((User?)null);
        userRepo.Setup(r => r.CreateUserAsync("newadmin", It.IsAny<string>(), "Admin")).ReturnsAsync(101);

        var controller = CreateUsersController(userRepo);
        var dto = new UserCreateDTO
        {
            Username = "newadmin",
            Password = "Password@123",
            Role = "Admin"
        };

        // Act
        var result = await controller.CreateUser(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        userRepo.Verify(r => r.CreateUserAsync("newadmin", It.IsAny<string>(), "Admin"), Times.Once);
    }

    [Fact]
    public async Task CreateUser_Should_Support_All_Three_Roles()
    {
        // Arrange
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        userRepo.Setup(r => r.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(1);

        var controller = CreateUsersController(userRepo);

        // Test each role
        foreach (var role in new[] { "Admin", "Manager", "Cashier" })
        {
            var dto = new UserCreateDTO
            {
                Username = $"user_{role}",
                Password = "Password@123",
                Role = role
            };

            // Act
            var result = await controller.CreateUser(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result);
            Assert.Equal(201, createdResult.StatusCode);
        }
    }

    [Fact]
    public async Task CreateUser_Should_Return_BadRequest_When_Username_Empty()
    {
        // Arrange
        var userRepo = new Mock<IUserRepository>();
        var controller = CreateUsersController(userRepo);
        var dto = new UserCreateDTO { Username = "   ", Password = "Password@123", Role = "Admin" };

        // Act
        var result = await controller.CreateUser(dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task CreateUser_Should_Return_BadRequest_When_Password_Too_Short()
    {
        // Arrange
        var userRepo = new Mock<IUserRepository>();
        var controller = CreateUsersController(userRepo);
        var dto = new UserCreateDTO { Username = "user", Password = "short", Role = "Admin" };

        // Act
        var result = await controller.CreateUser(dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task CreateUser_Should_Return_BadRequest_When_Invalid_Role()
    {
        // Arrange
        var userRepo = new Mock<IUserRepository>();
        var controller = CreateUsersController(userRepo);
        var dto = new UserCreateDTO { Username = "user", Password = "Password@123", Role = "InvalidRole" };

        // Act
        var result = await controller.CreateUser(dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task CreateUser_Should_Return_Conflict_When_Username_Exists()
    {
        // Arrange
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByUsernameAsync("existing")).ReturnsAsync(new User { Id = 1, Username = "existing" });

        var controller = CreateUsersController(userRepo);
        var dto = new UserCreateDTO { Username = "existing", Password = "Password@123", Role = "Admin" };

        // Act
        var result = await controller.CreateUser(dto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflictResult.StatusCode);
    }

    [Fact]
    public async Task CreateUser_Should_Hash_Password_Before_Storage()
    {
        // Arrange
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByUsernameAsync("user")).ReturnsAsync((User?)null);
        userRepo.Setup(r => r.CreateUserAsync("user", It.IsAny<string>(), "Admin")).ReturnsAsync(1);

        var passwordHasher = new Mock<IPasswordHasher>();
        var hashedPassword = "";
        passwordHasher.Setup(p => p.HashPassword("Password@123"))
            .Returns((string pwd) =>
            {
                hashedPassword = $"hashed_{pwd}";
                return hashedPassword;
            });

        var jwtService = new Mock<IJwtTokenService>();
        jwtService.Setup(s => s.GenerateToken(It.IsAny<User>())).Returns(new AuthResponseDTO());

        var controller = new UsersController(userRepo.Object, passwordHasher.Object, jwtService.Object);
        var dto = new UserCreateDTO { Username = "user", Password = "Password@123", Role = "Admin" };

        // Act
        var result = await controller.CreateUser(dto);

        // Assert
        passwordHasher.Verify(p => p.HashPassword("Password@123"), Times.Once);
    }

    #endregion

    #region Story S3-US-08: Role Assignment

    [Fact]
    public async Task UpdateUserRole_Should_Change_Role_When_Valid()
    {
        // Arrange
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1, Username = "user", Role = "Cashier" });
        userRepo.Setup(r => r.UpdateRoleAsync(1, "Manager")).ReturnsAsync(true);

        var controller = CreateUsersController(userRepo);
        var dto = new UserRoleUpdateDTO { Role = "Manager" };

        // Act
        var result = await controller.UpdateUserRole(1, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        userRepo.Verify(r => r.UpdateRoleAsync(1, "Manager"), Times.Once);
    }

    [Fact]
    public async Task UpdateUserRole_Should_Return_NotFound_When_User_Not_Exists()
    {
        // Arrange
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        var controller = CreateUsersController(userRepo);
        var dto = new UserRoleUpdateDTO { Role = "Manager" };

        // Act
        var result = await controller.UpdateUserRole(999, dto);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public async Task UpdateUserRole_Should_Return_BadRequest_When_Invalid_Role()
    {
        // Arrange
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1, Username = "user", Role = "Cashier" });

        var controller = CreateUsersController(userRepo);
        var dto = new UserRoleUpdateDTO { Role = "InvalidRole" };

        // Act
        var result = await controller.UpdateUserRole(1, dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task UpdateUserRole_Should_Normalize_Role_Case_Insensitive()
    {
        // Arrange
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1, Username = "user", Role = "Cashier" });
        userRepo.Setup(r => r.UpdateRoleAsync(1, It.IsAny<string>())).ReturnsAsync(true);

        var controller = CreateUsersController(userRepo);
        var dto = new UserRoleUpdateDTO { Role = "manager" };  // lowercase

        // Act
        var result = await controller.UpdateUserRole(1, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    #endregion

    #region Story S3-US-09: User Management Dashboard

    [Fact]
    public async Task GetUsers_Should_Return_All_Users()
    {
        // Arrange
        var userRepo = new Mock<IUserRepository>();
        var users = new List<User>
        {
            new() { Id = 1, Username = "admin", Role = "Admin", CreatedAt = System.DateTime.UtcNow },
            new() { Id = 2, Username = "manager", Role = "Manager", CreatedAt = System.DateTime.UtcNow },
            new() { Id = 3, Username = "cashier", Role = "Cashier", CreatedAt = System.DateTime.UtcNow }
        };

        userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        var controller = CreateUsersController(userRepo);

        // Act
        var result = await controller.GetUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedUsers = ((IEnumerable<dynamic>)okResult.Value!).ToList();
        Assert.Equal(3, returnedUsers.Count);
    }

    [Fact]
    public async Task GetUsers_Should_Include_Id_Username_Role_CreatedAt()
    {
        // Arrange
        var createdAt = System.DateTime.UtcNow;
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
        {
            new() { Id = 1, Username = "testuser", Role = "Admin", CreatedAt = createdAt }
        });

        var controller = CreateUsersController(userRepo);

        // Act
        var result = await controller.GetUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var users = ((IEnumerable<dynamic>)okResult.Value!).ToList();
        var firstUser = users.First();

        // Verify required fields are present
        Assert.NotNull(firstUser.id);
        Assert.NotNull(firstUser.username);
        Assert.NotNull(firstUser.role);
        Assert.NotNull(firstUser.createdAt);
    }

    [Fact]
    public async Task GetUsers_Should_Return_Empty_List_When_No_Users()
    {
        // Arrange
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

        var controller = CreateUsersController(userRepo);

        // Act
        var result = await controller.GetUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var users = ((IEnumerable<dynamic>)okResult.Value!).ToList();
        Assert.Empty(users);
    }

    [Fact]
    public async Task DeleteUser_Should_Remove_User_When_Valid()
    {
        // Arrange
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        var controller = CreateUsersController(userRepo);

        // Act
        var result = await controller.DeleteUser(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        userRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_Should_Return_NotFound_When_User_Not_Exists()
    {
        // Arrange
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.DeleteAsync(999)).ReturnsAsync(false);

        var controller = CreateUsersController(userRepo);

        // Act
        var result = await controller.DeleteUser(999);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    #endregion
}
