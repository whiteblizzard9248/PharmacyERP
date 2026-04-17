using MediatR;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Features.Auth;

public sealed class LoginCommand(LoginDto loginDto) : IRequest<LoginResultDto>
{
    public LoginDto LoginDto { get; } = loginDto;
}
