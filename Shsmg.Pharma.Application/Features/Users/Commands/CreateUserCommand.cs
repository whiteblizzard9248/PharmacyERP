using MediatR;

namespace Shsmg.Pharma.Application.Features.Users.Commands;

public sealed class CreateUserCommand : IRequest<Unit>
{
    public CreateUserCommand(string email, string password, List<string> roles)
    {
        Email = email;
        Password = password;
        Roles = roles;
    }

    public string Email { get; }
    public string Password { get; }
    public List<string> Roles { get; }
}