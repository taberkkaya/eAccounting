using eAccountingServer.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Auth;
public sealed record ConfirmEmailCommand(
    Guid UserId,
    string Token) : IRequest<Result<string>>;

internal sealed class ConfirmEmailCommandHandler(
    UserManager<AppUser> userManager
    ) : IRequestHandler<ConfirmEmailCommand, Result<string>>
{
    public async Task<Result<string>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        AppUser? user = await userManager.Users.FirstOrDefaultAsync(p => p.Id == request.UserId, cancellationToken);
        if (user is null)
            return Result<string>.Failure("Kullanıcı bulunamadı.");

        if (user.EmailConfirmed)
            return "Bu e-posta adresi zaten doğrulanmış.";

        // Onayı ConfirmEmailAsync jetonu doğruladıktan sonra kendisi yazıyor.
        // Burada baştan işaretlemek, geçersiz jetonla gelen birinin de doğrulanmış
        // sayılabilmesi demekti: takip edilen varlık daha sonraki bir kayıtla
        // veritabanına geçebilirdi.
        IdentityResult result = await userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
            return Result<string>.Failure(result.Errors.Select(s => s.Description).ToList());

        return "E-posta adresiniz doğrulandı.";
    }
}
