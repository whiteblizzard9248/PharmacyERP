using MediatR;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.Common;

namespace Shsmg.Pharma.Application.Features.Users.Commands;

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Unit>
{
    private readonly IUserService _userService;

    public UpdateUserCommandHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<Unit> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        await _userService.UpdateUserAsync(request.UserId, request.Email, request.Roles);
        return Unit.Value;
    }
}