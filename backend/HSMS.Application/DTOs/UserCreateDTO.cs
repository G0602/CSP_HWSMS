namespace HSMS.Application.DTOs;

public class UserCreateDTO
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = "Cashier";
}
