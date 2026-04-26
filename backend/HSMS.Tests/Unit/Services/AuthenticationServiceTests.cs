using HSMS.API.Services;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using HSMS.Domain.Entities;
using Moq;

namespace HSMS.Tests;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task LoginAsync_Should_Trim_Username_And_Return_Token_When_Credentials_Are_Valid()
    {
        var user = new User
        {
            Id = 7,
            Username = "manager",
            PasswordHash = "stored-hash",
            Role = "Manager"
        };
        var userRepository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtTokenService = new Mock<IJwtTokenService>();

        userRepository.Setup(repo => repo.GetByUsernameAsync("manager")).ReturnsAsync(user);
        passwordHasher.Setup(service => service.VerifyPassword("Password@123", "stored-hash")).Returns(true);
        jwtTokenService.Setup(service => service.GenerateToken(user)).Returns(new AuthResponseDTO
        {
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role,
            AccessToken = "token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        });

        var service = new AuthenticationService(userRepository.Object, passwordHasher.Object, jwtTokenService.Object);

        var result = await service.LoginAsync(new AuthLoginDTO
        {
            Username = "  manager  ",
            Password = "Password@123"
        });

        Assert.Equal(7, result.UserId);
        Assert.Equal("manager", result.Username);
        Assert.Equal("Manager", result.Role);
        Assert.Equal("token", result.AccessToken);
        userRepository.Verify(repo => repo.GetByUsernameAsync("manager"), Times.Once);
    }

    [Theory]
    [InlineData("", "Password@123")]
    [InlineData("   ", "Password@123")]
    [InlineData("cashier", "")]
    [InlineData("cashier", "   ")]
    public async Task LoginAsync_Should_Reject_Missing_Credentials(string username, string password)
    {
        var service = new AuthenticationService(
            Mock.Of<IUserRepository>(),
            Mock.Of<IPasswordHasher>(),
            Mock.Of<IJwtTokenService>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync(new AuthLoginDTO
        {
            Username = username,
            Password = password
        }));

        Assert.Equal("Username and password are required.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_Unauthorized_When_User_Does_Not_Exist()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(repo => repo.GetByUsernameAsync("missing")).ReturnsAsync((User?)null);

        var service = new AuthenticationService(
            userRepository.Object,
            Mock.Of<IPasswordHasher>(),
            Mock.Of<IJwtTokenService>());

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LoginAsync(new AuthLoginDTO
        {
            Username = "missing",
            Password = "Password@123"
        }));

        Assert.Equal("Invalid username or password.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_Unauthorized_When_Password_Does_Not_Verify()
    {
        var user = new User
        {
            Id = 4,
            Username = "cashier",
            PasswordHash = "stored-hash",
            Role = "Cashier"
        };
        var userRepository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtTokenService = new Mock<IJwtTokenService>();

        userRepository.Setup(repo => repo.GetByUsernameAsync("cashier")).ReturnsAsync(user);
        passwordHasher.Setup(service => service.VerifyPassword("wrong", "stored-hash")).Returns(false);

        var service = new AuthenticationService(userRepository.Object, passwordHasher.Object, jwtTokenService.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LoginAsync(new AuthLoginDTO
        {
            Username = "cashier",
            Password = "wrong"
        }));

        jwtTokenService.Verify(service => service.GenerateToken(It.IsAny<User>()), Times.Never);
    }
}
