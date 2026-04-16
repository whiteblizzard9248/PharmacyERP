using MediatR;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Features.Users.Queries;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IEnumerable<UserDto>>
{
    private readonly IUserService _userService;

    public GetUsersQueryHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<IEnumerable<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        return await _userService.GetUsersAsync();
    }
}