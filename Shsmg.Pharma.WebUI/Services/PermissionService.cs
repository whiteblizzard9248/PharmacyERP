using Microsoft.AspNetCore.Authorization;
using Shsmg.Pharma.Application.Common;

namespace Shsmg.Pharma.WebUI.Services;

public class PermissionService(IAuthorizationService authorizationService, IHttpContextAccessor httpContextAccessor)
{
    private readonly IAuthorizationService _authorizationService = authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task<bool> HasPermissionAsync(string permission)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null) return false;

        var result = await _authorizationService.AuthorizeAsync(user, null, permission);
        return result.Succeeded;
    }

    public async Task<bool> HasAnyPermissionAsync(params string[] permissions)
    {
        foreach (var permission in permissions)
        {
            if (await HasPermissionAsync(permission)) return true;
        }
        return false;
    }

    public Task<bool> IsInRoleAsync(string role)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return Task.FromResult(user?.IsInRole(role) == true);
    }

    public Task<bool> IsManagerAsync() => IsInRoleAsync(Roles.Manager);

    public Task<bool> IsAdminAsync() => IsInRoleAsync(Roles.Admin);
}
