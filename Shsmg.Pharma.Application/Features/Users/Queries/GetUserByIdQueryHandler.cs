using MediatR;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Features.Users.Queries;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IUserService _userService;

    public GetUserByIdQueryHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        return await _userService.GetUserByIdAsync(request.UserId);
    }
}