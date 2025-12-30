using Fourm.Models;

namespace Fourm.Services;

/// <summary>
/// Interface for user management service
/// </summary>
public interface IUserService
{
    Task<bool> RegisterUserAsync(string username, string email, string password, string major);
    Task<User?> ValidateUserAsync(string username, string password);
    Task<bool> UserExistsAsync(string username);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<List<User>> GetAllUsersAsync();
    Task<bool> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(string userId);
}
