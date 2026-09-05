using eAccountingServer.Domain.Mail;
using eAccountingServer.Domain.Users;
using FluentEmail.Core;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace eAccountingServer.Domain.Event;

public sealed class SendConfirmEmailEvent(
    UserManager<AppUser> userManager,
    IFluentEmail fluentEmail,
    IOptions<MailOptions> mailOptions
    ) : INotificationHandler<AppUserEvent>
{
    public async Task Handle(AppUserEvent notification, CancellationToken cancellationToken)
    {
        AppUser? user = await userManager.FindByIdAsync(notification.UserId.ToString());
        if (user is null) return;

        string token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        token = Uri.EscapeDataString(token);

        // Bağlantı uygulamanın yayınlandığı adrese kurulmalı; sabit localhost
        // alıcının kendi makinesini gösterirdi.
        string baseUrl = mailOptions.Value.ClientBaseUrl.TrimEnd('/');
        string confirmUrl =
            $"{baseUrl}/confirm-email?email={Uri.EscapeDataString(user.Email ?? string.Empty)}&token={token}";

        await fluentEmail
            .To(user.Email)
            .Subject("E-posta adresinizi doğrulayın")
            .Body(
                $"""
                <div style="font-family:Arial,Helvetica,sans-serif;font-size:15px;color:#222">
                  <h2 style="margin:0 0 12px">E-posta adresinizi doğrulayın</h2>
                  <p>Merhaba {user.FirstName},</p>
                  <p>
                    Hesabınızı kullanmaya başlamak için aşağıdaki bağlantıya tıklayarak
                    <strong>{user.Email}</strong> adresini doğrulayın.
                  </p>
                  <p style="margin:22px 0">
                    <a href="{confirmUrl}"
                       style="background:#050505;color:#d8ff36;padding:12px 20px;text-decoration:none;font-weight:bold">
                      E-postamı doğrula
                    </a>
                  </p>
                  <p style="color:#666;font-size:13px">
                    Bağlantı çalışmazsa bu adresi tarayıcınıza yapıştırabilirsiniz:<br>
                    {confirmUrl}
                  </p>
                  <p style="color:#666;font-size:13px">
                    Bu isteği siz yapmadıysanız bu maili yok sayabilirsiniz.
                  </p>
                </div>
                """,
                isHtml: true)
            .SendAsync(cancellationToken);
    }
}
