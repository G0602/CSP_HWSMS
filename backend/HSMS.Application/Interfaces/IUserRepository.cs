using HSMS.Domain.Entities;

namespace HSMS.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(int id);
    Task<List<User>> GetAllAsync();

    Task<int> CreateUserAsync(string username, string passwordHash, string role);
    Task<bool> UpdateRoleAsync(int id, string role);
    Task<bool> DeleteAsync(int id);
}
