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
    ICacheService cacheService,
    IDemoContext demoContext
    ) : IRequestHandler<GetAllUsersQuery, Result<List<AppUser>>>
{
    public async Task<Result<List<AppUser>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        // A demo visitor shares the API with real accounts, so the user list is narrowed
        // to the shared demo identity rather than everybody in the database.
        if (demoContext.IsDemoRequest)
        {
            return await userManager.Users
                .Include(p => p.CompanyUsers!)
                .ThenInclude(p => p.Company)
                .Where(p => p.UserName == "demo")
                .ToListAsync(cancellationToken);
        }

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
