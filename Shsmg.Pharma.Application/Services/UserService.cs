using Shsmg.Pharma.Application.DTOs;
using IUserServiceInner = Shsmg.Pharma.Application.Common.IUserService;

namespace Shsmg.Pharma.Application.Services;

public sealed class UserService(IUserServiceInner innerUserService) : IUserService
{
    private readonly IUserServiceInner _userService = innerUserService;

    public Task<IEnumerable<UserDto>> GetUsersAsync() => _userService.GetUsersAsync();
    public Task<UserDto?> GetUserByIdAsync(string userId) => _userService.GetUserByIdAsync(userId);
    public Task CreateUserAsync(string email, string password, string[] roleIds) => _userService.CreateUserAsync(email, password, roleIds.ToList());
    public Task UpdateUserAsync(string userId, string email, string[] roleIds) => _userService.UpdateUserAsync(userId, email, roleIds.ToList());
    public Task DeleteUserAsync(string userId) => _userService.DeleteUserAsync(userId);
}
