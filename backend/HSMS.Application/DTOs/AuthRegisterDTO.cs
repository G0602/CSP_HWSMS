namespace HSMS.Application.DTOs;

public class AuthRegisterDTO
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = "User";
}
