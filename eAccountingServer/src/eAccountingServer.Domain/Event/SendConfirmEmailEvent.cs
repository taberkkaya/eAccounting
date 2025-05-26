using eAccountingServer.Domain.Users;
using FluentEmail.Core;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace eAccountingServer.Domain.Event;

public sealed class SendConfirmEmailEvent(
    UserManager<AppUser> userManager,
    IFluentEmail fluentEmail
    ) : INotificationHandler<AppUserEvent>
{
    public async Task Handle(AppUserEvent notification, CancellationToken cancellationToken)
    {
        AppUser? user = await userManager.FindByIdAsync(notification.UserId.ToString());
        if(user is not null)
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            token = Uri.EscapeDataString(token); // Ensure the token is URL-safe
            await fluentEmail
                .To(user.Email)
                .Subject("Confirm Email")
                .Body($"" +
                $"<h1>" +
                $"Confirm your email: {user.Email}" +
                $"</h1>" +
                $"<br>" +
                $"<a href='http://localhost:4200/confirm-email?email={user.Email}&token={token}' target='_blank'>Maili Onaylamak İçin Tıklayın</a>", true)
                .SendAsync(cancellationToken);
        }
    }
}
