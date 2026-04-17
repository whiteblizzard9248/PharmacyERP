using MediatR;

namespace Shsmg.Pharma.Application.Features.Users.Commands;

public sealed class CreateUserCommand(string email, string password, List<string> roles) : IRequest<Unit>
{
    public string Email { get; } = email;
    public string Password { get; } = password;
    public List<string> Roles { get; } = roles;
}