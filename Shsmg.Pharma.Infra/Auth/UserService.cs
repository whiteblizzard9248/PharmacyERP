using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Infra.Auth;

public sealed class UserService(UserManager<AppUser> userManager, IHttpContextAccessor httpContextAccessor) : IUserService
{
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task<IEnumerable<UserDto>> GetUsersAsync()
    {
        var users = _userManager.Users
            .Where(user => user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow)
            .ToList();
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                Roles = [.. roles]
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
            Email = user.Email!,
            Roles = [.. roles]
        };
    }

    public async Task CreateUserAsync(string email, string password, List<string> roles)
    {
        ValidateRoleAssignment(roles);

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
        ValidateRoleAssignment(roles);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found.");
        }

        await EnsureTargetManageableAsync(user);

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

    public async Task DeleteUserAsync(string userId)
    {
        if (!IsCurrentUserManager())
        {
            throw new UnauthorizedAccessException("Only managers may delete users.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(Roles.Admin) || roles.Contains(Roles.Manager))
        {
            throw new UnauthorizedAccessException("Managers may only delete employees.");
        }

        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private void ValidateRoleAssignment(List<string> roles)
    {
        if (IsCurrentUserManager())
        {
            if (roles.Any(r => r != Roles.Employee))
            {
                throw new UnauthorizedAccessException("Managers may only assign the Employee role.");
            }
        }
        else if (IsCurrentUserAdmin())
        {
            if (roles.Any(r => r == Roles.Admin))
            {
                throw new UnauthorizedAccessException("Admins may only assign Manager or Employee roles.");
            }
        }
    }

    private async Task EnsureTargetManageableAsync(AppUser user)
    {
        if (IsCurrentUserManager())
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(Roles.Admin) || roles.Contains(Roles.Manager))
            {
                throw new UnauthorizedAccessException("Managers may only manage employees.");
            }
        }
    }

    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private bool IsCurrentUserAdmin()
    {
        return _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Admin) == true;
    }

    private bool IsCurrentUserManager()
    {
        return _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Manager) == true;
    }
}
