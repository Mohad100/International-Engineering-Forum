using Fourm.Models;
using System.Security.Cryptography;
using System.Text;

namespace Fourm.Services;

/// <summary>
/// In-memory user service for demonstration purposes
/// In a real application, this would be replaced with a database
/// </summary>
public class InMemoryUserService : IUserService
{
    private readonly List<User> _users = new();
    private readonly object _lock = new();

    public Task<bool> RegisterUserAsync(string username, string email, string password)
    {
        lock (_lock)
        {
            // Check if user already exists
            if (_users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult(false);
            }

            // Create new user with hashed password
            var user = new User
            {
                Username = username,
                Email = email ?? string.Empty,
                PasswordHash = HashPassword(password)
            };

            _users.Add(user);
            return Task.FromResult(true);
        }
    }

    public Task<User?> ValidateUserAsync(string username, string password)
    {
        lock (_lock)
        {
            var user = _users.FirstOrDefault(u => 
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user != null && VerifyPassword(password, user.PasswordHash))
            {
                return Task.FromResult<User?>(user);
            }

            return Task.FromResult<User?>(null);
        }
    }

    public Task<bool> UserExistsAsync(string username)
    {
        lock (_lock)
        {
            return Task.FromResult(_users.Any(u => 
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<User?> GetUserByUsernameAsync(string username)
    {
        lock (_lock)
        {
            var user = _users.FirstOrDefault(u => 
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(user);
        }
    }

    public Task<List<User>> GetAllUsersAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_users.OrderByDescending(u => u.CreatedAt).ToList());
        }
    }

    public Task<bool> UpdateUserAsync(User user)
    {
        lock (_lock)
        {
            var existingUser = _users.FirstOrDefault(u => u.Id == user.Id);
            if (existingUser != null)
            {
                existingUser.Username = user.Username;
                existingUser.Email = user.Email;
                existingUser.PasswordHash = user.PasswordHash;
                existingUser.IsAdmin = user.IsAdmin;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Simple password hashing using SHA256
    /// Note: In production, use BCrypt, Argon2, or PBKDF2
    /// </summary>
    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    /// <summary>
    /// Verify password against hash
    /// </summary>
    private static bool VerifyPassword(string password, string hash)
    {
        var passwordHash = HashPassword(password);
        return passwordHash == hash;
    }
}
