using MediatR;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Features.Auth;

public sealed class LoginCommand : IRequest<LoginResultDto>
{
    public LoginCommand(LoginDto loginDto)
    {
        LoginDto = loginDto;
    }

    public LoginDto LoginDto { get; }
}
