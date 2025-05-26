using System.Windows.Markup;
using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.User;
public sealed record GetAllUsersQuery() : IRequest<Result<List<AppUser>>>;

internal sealed class GetAllUsersQueryHandler(
    UserManager<AppUser> userManager,
    ICacheService cacheService
    ) : IRequestHandler<GetAllUsersQuery, Result<List<AppUser>>>
{
    public async Task<Result<List<AppUser>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        List<AppUser>? users;

        users = cacheService.Get<List<AppUser>>("users");

        if (users is null)
        {
            users = await userManager
            .Users
            .Include(p => p.CompanyUsers!)
            .ThenInclude(p => p.Company)
            .OrderBy(p => p.FirstName)
            .ToListAsync(cancellationToken);

            cacheService.Set<List<AppUser>>("users", users);
        }

        if (users is null)
            return Result<List<AppUser>>.Failure("Users not found!");

        return users;
    }
}
