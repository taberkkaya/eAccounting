using System.Windows.Markup;
using eAccountingServer.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.User;
public sealed record GetAllUsersQuery() : IRequest<Result<List<AppUser>>>;

internal sealed class GetAllUsersQueryHandler(
    UserManager<AppUser> userManager
    ) : IRequestHandler<GetAllUsersQuery, Result<List<AppUser>>>
{
    public async Task<Result<List<AppUser>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        List<AppUser> users = await userManager.Users.OrderBy(p => p.FirstName).ToListAsync(cancellationToken);
        
        if (users is null)
            return Result<List<AppUser>>.Failure("Users not found!");

        return users;
    }
}
