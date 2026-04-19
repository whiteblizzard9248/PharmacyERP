using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Services;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetUsersAsync();
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task CreateUserAsync(string email, string password, string[] roleIds);
    Task UpdateUserAsync(string userId, string email, string[] roleIds);
    Task DeleteUserAsync(string userId);
}
