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
        // Adresin bağlantıda taşınmasına gerek yok: kimlik zaten kullanıcıyı buluyor.
        // Adres URL'de görününce hem tarayıcı geçmişine ve sunucu kayıtlarına
        // düşüyor, hem de "adres + uzun jeton" kalıbı kimlik avı filtrelerinin
        // aradığı şeye benziyor.
        string baseUrl = mailOptions.Value.ClientBaseUrl.TrimEnd('/');
        string confirmUrl = $"{baseUrl}/confirm-email?user={user.Id}&token={token}";

        string body = MailTemplate.Wrap(
            "E-posta adresinizi doğrulayın",
            MailTemplate.Paragraph($"Merhaba <strong style=\"color:#0f172a\">{user.FirstName}</strong>,")
            + MailTemplate.Paragraph(
                $"Defter hesabınızı kullanmaya başlamak için <strong style=\"color:#0f172a\">{user.Email}</strong> "
                + "adresinin size ait olduğunu doğrulayın.")
            + MailTemplate.Button("E-postamı doğrula", confirmUrl)
            + MailTemplate.FallbackLink(confirmUrl)
            + MailTemplate.Note("Bu isteği siz yapmadıysanız bu maili yok sayabilirsiniz; hesabınızda hiçbir şey değişmez."),
            baseUrl);

        await fluentEmail
            .To(user.Email)
            .Subject("Defter — e-posta adresinizi doğrulayın")
            .Body(body, isHtml: true)
            .SendAsync(cancellationToken);
    }
}
