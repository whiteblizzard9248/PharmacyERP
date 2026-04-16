using MediatR;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Features.Users.Queries;

public sealed class GetUsersQuery : IRequest<IEnumerable<UserDto>>
{
}