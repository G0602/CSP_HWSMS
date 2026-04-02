namespace HSMS.Domain.Entities;

/// <summary>
/// Represents an application user that can authenticate into the system.
/// </summary>
public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "Cashier";

    public DateTime CreatedAt { get; set; }
}
