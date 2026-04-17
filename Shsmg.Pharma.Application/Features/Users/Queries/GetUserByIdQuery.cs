using MediatR;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Features.Users.Queries;

public sealed class GetUserByIdQuery(string userId) : IRequest<UserDto?>
{
    public string UserId { get; } = userId;
}