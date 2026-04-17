using MediatR;

namespace Shsmg.Pharma.Application.Features.Users.Commands;

public sealed class DeleteUserCommand(string userId) : IRequest<Unit>
{
    public string UserId { get; } = userId;
}
