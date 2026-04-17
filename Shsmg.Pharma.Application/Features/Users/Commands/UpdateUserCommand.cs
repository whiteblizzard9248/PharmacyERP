using MediatR;

namespace Shsmg.Pharma.Application.Features.Users.Commands;

public sealed class UpdateUserCommand(string userId, string email, List<string> roles) : IRequest<Unit>
{
    public string UserId { get; } = userId;
    public string Email { get; } = email;
    public List<string> Roles { get; } = roles;
}