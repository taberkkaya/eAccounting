using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
            return Result<string>.Failure("Kullanıcı bulunamadı.");

        // Losing the last administrator locks everyone out of user management with no
        // way back in, so the account has to be replaced before it can be removed.
        if (user.IsAdmin && !await AnotherAdminExistsAsync(user.Id, cancellationToken))
            return Result<string>.Failure(
                "Sistemdeki son yönetici silinemez. Önce başka bir yönetici oluşturun.");

        user.IsDeleted = true;
        IdentityResult result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Result<string>.Failure(result.Errors.Select(s => s.Description).ToList());

        cacheService.Remove("users");

        return "Kullanıcı silindi.";
    }

    private Task<bool> AnotherAdminExistsAsync(Guid userId, CancellationToken cancellationToken) =>
        userManager.Users.AnyAsync(p => p.IsAdmin && p.Id != userId, cancellationToken);
}
