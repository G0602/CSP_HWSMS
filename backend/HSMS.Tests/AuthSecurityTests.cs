using HSMS.API.Services;

namespace HSMS.Tests;

public class AuthSecurityTests
{
    [Fact]
    public void HashPassword_Should_Not_Store_PlainText_And_Should_Verify()
    {
        var hasher = new PasswordHasher();

        const string password = "MySecurePassword123!";
        string hash = hasher.HashPassword(password);

        Assert.NotEqual(password, hash);
        Assert.True(hasher.VerifyPassword(password, hash));
        Assert.False(hasher.VerifyPassword("wrong-password", hash));
    }
}
