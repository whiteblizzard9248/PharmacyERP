using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Shsmg.Pharma.Application.Common;

namespace Shsmg.Pharma.Infra.Auth;

public sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public string GetCurrentUserIdentifier()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return "system";
        }

        return user.FindFirstValue(ClaimTypes.Email)
            ?? user.Identity?.Name
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "system";
    }
}
