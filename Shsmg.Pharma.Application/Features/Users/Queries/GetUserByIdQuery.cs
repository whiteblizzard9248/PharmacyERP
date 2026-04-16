using MediatR;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Features.Users.Queries;

public sealed class GetUserByIdQuery : IRequest<UserDto?>
{
    public GetUserByIdQuery(string userId)
    {
        UserId = userId;
    }

    public string UserId { get; }
}