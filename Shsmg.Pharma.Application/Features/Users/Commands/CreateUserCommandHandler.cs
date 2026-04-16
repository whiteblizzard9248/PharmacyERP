using MediatR;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.Common;

namespace Shsmg.Pharma.Application.Features.Users.Commands;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Unit>
{
    private readonly IUserService _userService;

    public CreateUserCommandHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<Unit> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        await _userService.CreateUserAsync(request.Email, request.Password, request.Roles);
        return Unit.Value;
    }
}