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
            .Subject("Defter — e-posta adresinizi doğrulayın")
            .Body(
                $"""
                <div style="font-family:Inter,Arial,Helvetica,sans-serif;font-size:15px;color:#0f172a">
                  <h2 style="margin:0 0 12px;font-size:20px">E-posta adresinizi doğrulayın</h2>
                  <p style="color:#475569">Merhaba {user.FirstName},</p>
                  <p style="color:#475569">
                    Defter hesabınızı kullanmaya başlamak için aşağıdaki düğmeye tıklayarak
                    <strong style="color:#0f172a">{user.Email}</strong> adresini doğrulayın.
                  </p>
                  <p style="margin:24px 0">
                    <a href="{confirmUrl}"
                       style="display:inline-block;background:#2563eb;color:#ffffff;
                              padding:13px 24px;border-radius:8px;text-decoration:none;
                              font-weight:600;font-size:15px">
                      E-postamı doğrula
                    </a>
                  </p>
                  <p style="color:#64748b;font-size:13px;line-height:1.6">
                    Düğme çalışmazsa bu adresi tarayıcınıza yapıştırabilirsiniz:<br>
                    <span style="color:#2563eb;word-break:break-all">{confirmUrl}</span>
                  </p>
                  <p style="color:#94a3b8;font-size:13px">
                    Bu isteği siz yapmadıysanız bu maili yok sayabilirsiniz.
                  </p>
                </div>
                """,
                isHtml: true)
            .SendAsync(cancellationToken);
    }
}
