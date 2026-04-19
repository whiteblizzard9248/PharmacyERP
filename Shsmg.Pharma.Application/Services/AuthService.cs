using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Services;

public sealed class AuthService(IIdentityService identityService) : IAuthService
{
    private readonly IIdentityService _identityService = identityService;

    public Task<LoginResultDto> LoginAsync(string email, string password, bool rememberMe)
    {
        return _identityService.LoginAsync(email, password, rememberMe);
    }

    public Task LogoutAsync()
    {
        return _identityService.LogoutAsync();
    }
}
