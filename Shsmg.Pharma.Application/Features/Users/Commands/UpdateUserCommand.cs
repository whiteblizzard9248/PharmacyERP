using MediatR;

namespace Shsmg.Pharma.Application.Features.Users.Commands;

public sealed class UpdateUserCommand : IRequest<Unit>
{
    public UpdateUserCommand(string userId, string email, List<string> roles)
    {
        UserId = userId;
        Email = email;
        Roles = roles;
    }

    public string UserId { get; }
    public string Email { get; }
    public List<string> Roles { get; }
}