using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using ResultKit;

namespace eAccountingServer.Application.Features.User;
public sealed record DeleteUserByIdCommand(
    Guid Id) : IRequest<Result<string>>;

internal sealed class DeleteUserByIdCommandHandler(
    UserManager<AppUser> userManager,
    ICacheService cacheService
    ) : IRequestHandler<DeleteUserByIdCommand, Result<string>>
{
    public async Task<Result<string>> Handle(DeleteUserByIdCommand request, CancellationToken cancellationToken)
    {
        AppUser? user = await userManager.FindByIdAsync(request.Id.ToString());
        if (user is null)
            return Result<string>.Failure("User not found!");

        user.IsDeleted = true;
        IdentityResult result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Result<string>.Failure(result.Errors.Select(s => s.Description).ToList());

        cacheService.Remove("users");

        return "User deleted successfully!";
    }
}
