using HSMS.Domain.Entities;

namespace HSMS.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);

    Task<int> CreateUserAsync(string username, string passwordHash, string role);
}
