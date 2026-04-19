using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Services;

public interface IAuthService
{
    Task<LoginResultDto> LoginAsync(string email, string password, bool rememberMe);
    Task LogoutAsync();
}
