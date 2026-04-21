using Microsoft.Extensions.Logging;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Services;

public sealed class AuthService(IIdentityService identityService, ILogger<AuthService> logger) : IAuthService
{
    private readonly IIdentityService _identityService = identityService;
    private readonly ILogger<AuthService> _logger = logger;

    public Task<LoginResultDto> LoginAsync(string email, string password, bool rememberMe)
    {
        _logger.LogInformation("Attempting to login user: {Email}", email);
        return _identityService.LoginAsync(email, password, rememberMe);
    }

    public Task LogoutAsync()
    {
        _logger.LogInformation("Logging out current user.");
        return _identityService.LogoutAsync();
    }
}
