using eAccountingServer.Domain.Event;
using eAccountingServer.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using ResultKit;

namespace eAccountingServer.Application.Auth;
public sealed record SendConfirmEmailCommand(string Email) : IRequest<Result<string>>;

internal sealed class SendConfirmEmailCommandHandler(
    IMediator mediator,
    UserManager<AppUser> userManager
    ) : IRequestHandler<SendConfirmEmailCommand, Result<string>>
{
    public async Task<Result<string>> Handle(SendConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        AppUser? user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Result<string>.Failure("Kullanıcı bulunamadı.");
        
        if(user.EmailConfirmed)
            return Result<string>.Failure("Bu e-posta adresi zaten doğrulanmış.");

        await mediator.Publish(new AppUserEvent(user.Id));
        return "Onay maili gönderildi.";
    }
}
