using Microsoft.AspNetCore.Identity;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Infra.Auth;

public sealed class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;

    public UserService(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IEnumerable<UserDto>> GetUsersAsync()
    {
        var users = _userManager.Users.ToList();
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName,
                Roles = roles.ToList()
            });
        }

        return userDtos;
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? user.UserName,
            Roles = roles.ToList()
        };
    }

    public async Task CreateUserAsync(string email, string password, List<string> roles)
    {
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        if (roles.Any())
        {
            var roleResult = await _userManager.AddToRolesAsync(user, roles);
            if (!roleResult.Succeeded)
            {
                throw new Exception(string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
        }
    }

    public async Task UpdateUserAsync(string userId, string email, List<string> roles)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found.");
        }

        // Update email if changed
        if (user.Email != email)
        {
            user.Email = email;
            user.UserName = email;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new Exception(string.Join(", ", updateResult.Errors.Select(e => e.Description)));
            }
        }

        // Update roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        var rolesToRemove = currentRoles.Except(roles).ToList();
        var rolesToAdd = roles.Except(currentRoles).ToList();

        if (rolesToRemove.Any())
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                throw new Exception(string.Join(", ", removeResult.Errors.Select(e => e.Description)));
            }
        }

        if (rolesToAdd.Any())
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                throw new Exception(string.Join(", ", addResult.Errors.Select(e => e.Description)));
            }
        }
    }
}