using MediatR;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Features.Auth;

public sealed class LoginCommandHandler(IIdentityService identityService) : IRequestHandler<LoginCommand, LoginResultDto>
{
    private readonly IIdentityService _identityService = identityService;

    public Task<LoginResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return _identityService.LoginAsync(request.LoginDto.Email, request.LoginDto.Password, request.LoginDto.RememberMe);
    }
}
