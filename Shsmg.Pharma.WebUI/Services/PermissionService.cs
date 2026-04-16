using Microsoft.AspNetCore.Authorization;
using Shsmg.Pharma.Application.Common;

namespace Shsmg.Pharma.WebUI.Services;

public class PermissionService
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionService(IAuthorizationService authorizationService, IHttpContextAccessor httpContextAccessor)
    {
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
    }

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
}