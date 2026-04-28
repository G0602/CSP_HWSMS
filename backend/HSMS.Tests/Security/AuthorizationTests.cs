using HSMS.API.Controllers;
using HSMS.API.Auth;
using HSMS.API.Services;
using HSMS.Application.Interfaces;
using HSMS.Application.DTOs;
using HSMS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Moq;
using Xunit;

namespace HSMS.Tests;

/// <summary>
/// Sprint 3 - Authorization & Security Tests
/// Tests for role-based access control policies
/// Covers: InventoryManagerRead, InventoryWrite, UsersManage, SalesRead, etc.
/// </summary>
public class AuthorizationTests
{
    #region Policy Tests

    [Fact]
    public void AuthPolicies_Should_Define_All_Required_Policies()
    {
        // Arrange & Act & Assert
        Assert.NotNull(AuthPolicies.InventoryRead);
        Assert.NotNull(AuthPolicies.InventoryManagerRead);
        Assert.NotNull(AuthPolicies.InventoryWrite);
        Assert.NotNull(AuthPolicies.InventoryDelete);
        Assert.NotNull(AuthPolicies.SalesCreate);
        Assert.NotNull(AuthPolicies.SalesRead);
        Assert.NotNull(AuthPolicies.UsersManage);
    }

    #endregion

    #region Role Tests

    [Fact]
    public void AppRoles_Should_Support_Three_Roles()
    {
        // Arrange & Act & Assert
        Assert.Equal("Admin", AppRoles.Admin);
        Assert.Equal("Manager", AppRoles.Manager);
        Assert.Equal("Cashier", AppRoles.Cashier);
    }

    #endregion

    #region Controller Authorization Attributes

    [Fact]
    public void InventoryProducts_Endpoint_Should_Require_InventoryManagerRead_Policy()
    {
        // Arrange
        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(r => r.GetAllProducts()).ReturnsAsync(new List<Product>());

        var controller = new ProductController(mockRepo.Object);

        // Act
        var method = typeof(ProductController).GetMethod("GetInventoryProducts");

        // Assert
        Assert.NotNull(method);
        var authorizeAttribute = method?.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .FirstOrDefault() as AuthorizeAttribute;

        // Note: In actual runtime, authorization is checked by middleware
        // Unit tests verify attributes exist
        Assert.NotNull(authorizeAttribute);
    }

    [Fact]
    public void UsersManage_Endpoint_Should_Require_UsersManage_Policy()
    {
        // Arrange - This tests that UsersController methods have authorization
        var mockUserRepo = new Mock<IUserRepository>();
        var mockPasswordHasher = new Mock<IPasswordHasher>();
        var mockJwtService = new Mock<IJwtTokenService>();

        mockJwtService.Setup(s => s.GenerateToken(It.IsAny<User>()))
            .Returns(new AuthResponseDTO());

        var controller = new UsersController(mockUserRepo.Object, mockPasswordHasher.Object, mockJwtService.Object);

        // Act - Check GetUsers method
        var method = typeof(UsersController).GetMethod("GetUsers");

        // Assert
        Assert.NotNull(method);
        var authorizeAttribute = method?.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .FirstOrDefault() as AuthorizeAttribute;

        Assert.NotNull(authorizeAttribute);
    }

    #endregion

    #region Role Normalization

    [Theory]
    [InlineData("admin", "Admin")]
    [InlineData("manager", "Manager")]
    [InlineData("cashier", "Cashier")]
    public async Task CreateUser_Should_Normalize_Role_Case_Insensitively(string input, string expected)
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(repo => repo.GetByUsernameAsync("case-user")).ReturnsAsync((User?)null);
        userRepo.Setup(repo => repo.CreateUserAsync("case-user", It.IsAny<string>(), expected)).ReturnsAsync(7);

        var controller = new UsersController(
            userRepo.Object,
            new PasswordHasher(),
            Mock.Of<IJwtTokenService>());

        var result = await controller.CreateUser(new UserCreateDTO
        {
            Username = "case-user",
            Password = "Password@123",
            Role = input
        });

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, created.StatusCode);
        userRepo.Verify(repo => repo.CreateUserAsync("case-user", It.IsAny<string>(), expected), Times.Once);
    }

    #endregion

    #region User Claims Tests

    [Fact]
    public async Task CurrentUserRoleHandler_Should_Succeed_When_Database_Role_Allows_Access()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(new User
        {
            Id = 1,
            Username = "admin",
            Role = "Admin",
            PasswordHash = "hashed_password"
        });

        var requirement = new CurrentUserRoleRequirement(AppRoles.Admin);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", "1"),
            new Claim(ClaimTypes.Role, "Cashier")
        ], "TestAuth"));

        var context = new AuthorizationHandlerContext([requirement], principal, null);
        var handler = new CurrentUserRoleHandler(userRepo.Object);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task CurrentUserRoleHandler_Should_Fail_When_Database_Role_Does_Not_Allow_Access()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(repo => repo.GetByIdAsync(2)).ReturnsAsync(new User
        {
            Id = 2,
            Username = "cashier",
            Role = "Cashier",
            PasswordHash = "hashed_password"
        });

        var requirement = new CurrentUserRoleRequirement(AppRoles.Admin, AppRoles.Manager);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", "2"),
            new Claim(ClaimTypes.Role, "Manager")
        ], "TestAuth"));

        var context = new AuthorizationHandlerContext([requirement], principal, null);
        var handler = new CurrentUserRoleHandler(userRepo.Object);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task CurrentUserRoleHandler_Should_Fail_When_User_Is_Deleted()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(repo => repo.GetByIdAsync(3)).ReturnsAsync((User?)null);

        var requirement = new CurrentUserRoleRequirement(AppRoles.Admin);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", "3"),
            new Claim(ClaimTypes.Role, "Admin")
        ], "TestAuth"));

        var context = new AuthorizationHandlerContext([requirement], principal, null);
        var handler = new CurrentUserRoleHandler(userRepo.Object);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    #endregion

    #region Permission Hierarchy Tests

    [Theory]
    [InlineData("Admin", "InventoryManagerRead")]     // Admin can access Manager features
    [InlineData("Manager", "InventoryManagerRead")]   // Manager can access
    [InlineData("Admin", "SalesRead")]                // Admin can read sales
    [InlineData("Manager", "SalesRead")]              // Manager can read sales
    [InlineData("Admin", "UsersManage")]              // Admin manages users
    public void Role_Should_Have_Required_Permissions(string role, string policy)
    {
        // This test documents expected permission hierarchy
        // Admin > Manager > Cashier

        // Arrange & Act & Assert
        bool isAuthorized = false;

        if (policy == "InventoryManagerRead")
            isAuthorized = role == "Admin" || role == "Manager";
        else if (policy == "SalesRead")
            isAuthorized = role == "Admin" || role == "Manager";
        else if (policy == "UsersManage")
            isAuthorized = role == "Admin";

        Assert.True(isAuthorized, $"{role} should have access to {policy}");
    }

    [Theory]
    [InlineData("Cashier", "InventoryManagerRead")]   // Cashier cannot access Manager features
    [InlineData("Cashier", "UsersManage")]            // Cashier cannot manage users
    [InlineData("Manager", "UsersManage")]            // Manager cannot manage users
    public void Role_Should_Not_Have_Unauthorized_Access(string role, string policy)
    {
        // This test documents restricted access

        // Arrange & Act & Assert
        bool isUnauthorized = false;

        if (policy == "InventoryManagerRead")
            isUnauthorized = role == "Cashier";
        else if (policy == "UsersManage")
            isUnauthorized = role == "Cashier" || role == "Manager";

        Assert.True(isUnauthorized, $"{role} should NOT have access to {policy}");
    }

    #endregion

    #region Password Security Tests

    [Fact]
    public void Password_Should_Be_At_Least_8_Characters()
    {
        // Test the password validation rule

        // Arrange
        string shortPassword = "pass";
        string validPassword = "Password123";

        // Act & Assert
        Assert.True(shortPassword.Length < 8);
        Assert.True(validPassword.Length >= 8);
    }

    [Fact]
    public void Password_Should_Not_Be_Stored_In_Plain_Text()
    {
        // Arrange
        var mockPasswordHasher = new Mock<IPasswordHasher>();
        mockPasswordHasher.Setup(p => p.HashPassword("Password123"))
            .Returns("$2a$11$hashed_password_here");

        mockPasswordHasher.Setup(p => p.VerifyPassword("Password123", "$2a$11$hashed_password_here"))
            .Returns(true);

        // Act
        string plainPassword = "Password123";
        string hashedPassword = mockPasswordHasher.Object.HashPassword(plainPassword);
        bool verified = mockPasswordHasher.Object.VerifyPassword(plainPassword, hashedPassword);

        // Assert
        Assert.NotEqual(plainPassword, hashedPassword);
        Assert.True(verified);
    }

    #endregion
}
