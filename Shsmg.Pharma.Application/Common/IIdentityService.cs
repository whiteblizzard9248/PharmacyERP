using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Common;

public interface IIdentityService
{
    Task<LoginResultDto> LoginAsync(string email, string password, bool rememberMe);
    Task LogoutAsync();
}
