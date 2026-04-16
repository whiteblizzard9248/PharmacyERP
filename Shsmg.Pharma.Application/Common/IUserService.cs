using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Common;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetUsersAsync();
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task CreateUserAsync(string email, string password, List<string> roles);
    Task UpdateUserAsync(string userId, string email, List<string> roles);
}