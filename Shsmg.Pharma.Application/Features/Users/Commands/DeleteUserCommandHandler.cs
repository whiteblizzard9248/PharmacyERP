using MediatR;
using Shsmg.Pharma.Application.Common;

namespace Shsmg.Pharma.Application.Features.Users.Commands;

public sealed class DeleteUserCommandHandler(IUserService userService) : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IUserService _userService = userService;

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        await _userService.DeleteUserAsync(request.UserId);
        return Unit.Value;
    }
}
