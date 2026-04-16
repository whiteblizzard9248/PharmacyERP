using Microsoft.AspNetCore.Identity;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Infra.Auth;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public IdentityService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<LoginResultDto> LoginAsync(string email, string password, bool rememberMe)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return new LoginResultDto
            {
                Succeeded = false,
                ErrorMessage = "Invalid email or password."
            };
        }

        var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: false);
        return new LoginResultDto
        {
            Succeeded = result.Succeeded,
            ErrorMessage = result.Succeeded ? null : "Invalid email or password."
        };
    }

    public Task LogoutAsync()
    {
        return _signInManager.SignOutAsync();
    }
}
