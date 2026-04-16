using MediatR;
using Shsmg.Pharma.Application.Common;

namespace Shsmg.Pharma.Application.Features.Auth;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IIdentityService _identityService;

    public LogoutCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await _identityService.LogoutAsync();
        return Unit.Value;
    }
}
