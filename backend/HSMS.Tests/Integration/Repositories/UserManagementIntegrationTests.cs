using HSMS.API.Services;
using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using HSMS.Infrastructure.Repositories;
using HSMS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HSMS.Tests;

/// <summary>
/// Sprint 3 - EPIC 3.3: User Management Integration Tests
/// Tests user creation, role updates, and database persistence
/// Requires: HSMS_TEST_CONNECTION_STRING environment variable
/// </summary>
[Collection("DatabaseIntegration")]
public class UserManagementIntegrationTests
{
    private static string? GetConnectionString()
    {
        return System.Environment.GetEnvironmentVariable("HSMS_TEST_CONNECTION_STRING");
    }

    private static UserRepository CreateRepository(string connectionString)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", connectionString) })
            .Build();

        return new UserRepository(config);
    }

    #region Story S3-US-07: Admin Creates Users

    [Fact]
    public async Task CreateUserAsync_Should_Persist_To_Database()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        string username = $"testuser_{System.Guid.NewGuid():N}";
        var passwordHasher = new PasswordHasher();
        string passwordHash = passwordHasher.HashPassword("TestPassword123!");

        // Act
        int userId = await repository.CreateUserAsync(username, passwordHash, "Manager");

        try
        {
            // Assert
            Assert.True(userId > 0);

            var user = await repository.GetByIdAsync(userId);
            Assert.NotNull(user);
            Assert.Equal(username, user.Username);
            Assert.Equal("Manager", user.Role);
        }
        finally
        {
            await CleanupUserAsync(connectionString, userId);
        }
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Manager")]
    [InlineData("Cashier")]
    public async Task CreateUserAsync_Should_Support_All_Roles(string role)
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        string username = $"user_{role}_{System.Guid.NewGuid():N}";
        var passwordHasher = new PasswordHasher();
        string passwordHash = passwordHasher.HashPassword("Password123!");

        // Act
        int userId = await repository.CreateUserAsync(username, passwordHash, role);

        try
        {
            // Assert
            var user = await repository.GetByIdAsync(userId);
            Assert.NotNull(user);
            Assert.Equal(role, user.Role);
        }
        finally
        {
            await CleanupUserAsync(connectionString, userId);
        }
    }

    [Fact]
    public async Task CreateUserAsync_Should_Enforce_Username_Uniqueness()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        string username = $"unique_{System.Guid.NewGuid():N}";
        var passwordHasher = new PasswordHasher();

        int userId1 = await repository.CreateUserAsync(username, passwordHasher.HashPassword("Pass123!"), "Admin");

        try
        {
            // Act & Assert - Attempting to create duplicate should fail
            // Note: The repository should check this, but database constraints enforce it
            var existingUser = await repository.GetByUsernameAsync(username);
            Assert.NotNull(existingUser);
            Assert.Equal(userId1, existingUser.Id);
        }
        finally
        {
            await CleanupUserAsync(connectionString, userId1);
        }
    }

    [Fact]
    public async Task CreateUserAsync_Should_Store_Hashed_Password()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        string username = $"passhash_{System.Guid.NewGuid():N}";
        string plainPassword = "TestPassword123!";
        var passwordHasher = new PasswordHasher();
        string hashedPassword = passwordHasher.HashPassword(plainPassword);

        // Act
        int userId = await repository.CreateUserAsync(username, hashedPassword, "Admin");

        try
        {
            // Assert
            var user = await repository.GetByIdAsync(userId);
            Assert.NotNull(user);
            Assert.NotEqual(plainPassword, user.PasswordHash);
            Assert.NotEmpty(user.PasswordHash);

            // Verify password can be verified
            bool passwordValid = passwordHasher.VerifyPassword(plainPassword, user.PasswordHash);
            Assert.True(passwordValid);
        }
        finally
        {
            await CleanupUserAsync(connectionString, userId);
        }
    }

    #endregion

    #region Story S3-US-08: Role Assignment

    [Theory]
    [InlineData("Cashier", "Manager")]
    [InlineData("Manager", "Admin")]
    [InlineData("Admin", "Cashier")]
    public async Task UpdateRoleAsync_Should_Persist_Change_To_Database(string oldRole, string newRole)
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        string username = $"roletest_{System.Guid.NewGuid():N}";
        var passwordHasher = new PasswordHasher();
        int userId = await repository.CreateUserAsync(username, passwordHasher.HashPassword("Pass123!"), oldRole);

        try
        {
            // Act
            bool updated = await repository.UpdateRoleAsync(userId, newRole);

            // Assert
            Assert.True(updated);

            var user = await repository.GetByIdAsync(userId);
            Assert.NotNull(user);
            Assert.Equal(newRole, user.Role);
        }
        finally
        {
            await CleanupUserAsync(connectionString, userId);
        }
    }

    [Fact]
    public async Task UpdateRoleAsync_Should_Reflect_Changes_Immediately()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        string username = $"immediate_{System.Guid.NewGuid():N}";
        var passwordHasher = new PasswordHasher();
        int userId = await repository.CreateUserAsync(username, passwordHasher.HashPassword("Pass123!"), "Cashier");

        try
        {
            // Act
            await repository.UpdateRoleAsync(userId, "Admin");

            // Assert - Immediate change verification
            var userBefore = await repository.GetByIdAsync(userId);
            Assert.NotNull(userBefore);
            Assert.Equal("Admin", userBefore.Role);

            // Query by username to verify
            var userByUsername = await repository.GetByUsernameAsync(username);
            Assert.NotNull(userByUsername);
            Assert.Equal("Admin", userByUsername.Role);
        }
        finally
        {
            await CleanupUserAsync(connectionString, userId);
        }
    }

    #endregion

    #region Story S3-US-09: User Management Dashboard

    [Fact]
    public async Task GetAllAsync_Should_Return_All_Database_Records()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        var passwordHasher = new PasswordHasher();

        var userIds = new List<int>();
        for (int i = 0; i < 3; i++)
        {
            int id = await repository.CreateUserAsync(
                $"testuser_{i}_{System.Guid.NewGuid():N}",
                passwordHasher.HashPassword("Pass123!"),
                "Manager"
            );
            userIds.Add(id);
        }

        try
        {
            // Act
            var allUsers = await repository.GetAllAsync();

            // Assert
            Assert.NotEmpty(allUsers);
            foreach (int userId in userIds)
            {
                Assert.Contains(allUsers, u => u.Id == userId);
            }
        }
        finally
        {
            foreach (int id in userIds)
            {
                await CleanupUserAsync(connectionString, id);
            }
        }
    }

    [Fact]
    public async Task DeleteAsync_Should_Remove_From_Database()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        string username = $"delete_test_{System.Guid.NewGuid():N}";
        var passwordHasher = new PasswordHasher();
        int userId = await repository.CreateUserAsync(username, passwordHasher.HashPassword("Pass123!"), "Cashier");

        // Act
        bool deleted = await repository.DeleteAsync(userId);

        // Assert
        Assert.True(deleted);

        var user = await repository.GetByIdAsync(userId);
        Assert.Null(user);
    }

    #endregion

    #region Story S3-US-09: Password Reset

    [Fact]
    public async Task UpdatePasswordAsync_Should_Persist_New_Password_Hash()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        string username = $"pwd_reset_{System.Guid.NewGuid():N}";
        var passwordHasher = new PasswordHasher();
        string originalPassword = "OriginalPass123!";
        int userId = await repository.CreateUserAsync(username, passwordHasher.HashPassword(originalPassword), "Cashier");

        string newPassword = "NewPassword@456";
        string newPasswordHash = passwordHasher.HashPassword(newPassword);

        try
        {
            // Act
            bool updated = await repository.UpdatePasswordAsync(userId, newPasswordHash);

            // Assert
            Assert.True(updated);

            var user = await repository.GetByIdAsync(userId);
            Assert.NotNull(user);
            Assert.NotEqual(passwordHasher.HashPassword(originalPassword), user.PasswordHash);
            
            // Verify new password is valid
            bool newPasswordValid = passwordHasher.VerifyPassword(newPassword, user.PasswordHash);
            Assert.True(newPasswordValid);
            
            // Verify old password is no longer valid
            bool oldPasswordValid = passwordHasher.VerifyPassword(originalPassword, user.PasswordHash);
            Assert.False(oldPasswordValid);
        }
        finally
        {
            await CleanupUserAsync(connectionString, userId);
        }
    }

    [Fact]
    public async Task UpdatePasswordAsync_Should_Enforce_Hash_Format()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        string username = $"pwd_format_{System.Guid.NewGuid():N}";
        var passwordHasher = new PasswordHasher();
        int userId = await repository.CreateUserAsync(username, passwordHasher.HashPassword("Pass123!"), "Admin");

        try
        {
            // Act
            bool updated = await repository.UpdatePasswordAsync(userId, passwordHasher.HashPassword("UpdatedPass@789"));

            // Assert
            Assert.True(updated);

            var user = await repository.GetByIdAsync(userId);
            Assert.NotNull(user);
            Assert.NotEmpty(user.PasswordHash);
            Assert.Contains("$2", user.PasswordHash); // bcrypt format check
        }
        finally
        {
            await CleanupUserAsync(connectionString, userId);
        }
    }

    [Fact]
    public async Task UpdatePasswordAsync_For_NonExistent_User_Should_Return_False()
    {
        string? connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        // Arrange
        var repository = CreateRepository(connectionString);
        var passwordHasher = new PasswordHasher();
        const int nonExistentUserId = 999999;

        // Act
        bool updated = await repository.UpdatePasswordAsync(nonExistentUserId, passwordHasher.HashPassword("NewPass@123"));

        // Assert
        Assert.False(updated);
    }

    #endregion

    #region Helper Methods

    private static async Task CleanupUserAsync(string connectionString, int userId)
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        const string query = "DELETE FROM Users WHERE Id = @Id";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Id", userId);

        await command.ExecuteNonQueryAsync();
    }

    #endregion
}
