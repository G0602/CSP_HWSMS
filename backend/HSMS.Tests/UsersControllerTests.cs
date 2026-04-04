using HSMS.API.Controllers;
using HSMS.API.Services;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using HSMS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HSMS.Tests;

public class UsersControllerTests
{
    [Fact]
    public async Task CreateUser_Should_Return_Created_When_Valid()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(repo => repo.GetByUsernameAsync("new-admin"))
            .ReturnsAsync((User?)null);
        userRepo.Setup(repo => repo.CreateUserAsync(
                "new-admin",
                It.IsAny<string>(),
                "Admin"))
            .ReturnsAsync(101);

        var controller = new UsersController(userRepo.Object, new PasswordHasher());

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
            It.Is<string>(hash => hash != dto.Password && hash.Split('.').Length == 3),
            "Admin"), Times.Once);
    }

    [Fact]
    public async Task CreateUser_Should_Return_BadRequest_When_Role_Invalid()
    {
        var userRepo = new Mock<IUserRepository>();
        var controller = new UsersController(userRepo.Object, new PasswordHasher());

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

        var controller = new UsersController(userRepo.Object, new PasswordHasher());

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
        userRepo.Setup(repo => repo.CreateUserAsync(
                "new-user",
                It.IsAny<string>(),
                "Cashier"))
            .ReturnsAsync(102);

        var controller = new UsersController(userRepo.Object, new PasswordHasher());

        var result = await controller.CreateUser(new UserCreateDTO
        {
            Username = "new-user",
            Password = "Password@123",
            Role = ""
        });

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, created.StatusCode);
    }
}
