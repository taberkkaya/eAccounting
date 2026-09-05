using System.Security.Cryptography;
using System.Text;
using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Demo;
using eAccountingServer.Domain.Mail;
using eAccountingServer.Infrastructure.Context;
using FluentEmail.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eAccountingServer.Infrastructure.Demo;

/// <inheritdoc />
internal sealed class DemoVerificationService(
    ApplicationDbContext context,
    IFluentEmail fluentEmail,
    IOptions<DemoOptions> demoOptions,
    IOptions<MailOptions> mailOptions,
    ILogger<DemoVerificationService> logger
    ) : IDemoVerificationService
{
    private readonly DemoOptions _demo = demoOptions.Value;
    private readonly MailOptions _mail = mailOptions.Value;

    public bool Required =>
        _demo.Enabled
        && _demo.RequireEmailVerification
        && !string.IsNullOrWhiteSpace(_mail.SmtpHost);

    // --- kod gönderimi ------------------------------------------------------

    public async Task<DemoVerificationResult> SendCodeAsync(
        string email, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        if (!TryNormalize(email, out string normalized, out string display))
            return DemoVerificationResult.Fail("Geçerli bir e-posta adresi yazın.");

        DemoVisitor? visitor = await context.DemoVisitors
            .FirstOrDefaultAsync(p => p.Email == normalized, cancellationToken);

        DateTimeOffset now = DateTimeOffset.Now;

        if (visitor is null)
        {
            visitor = new DemoVisitor { Email = normalized };
            await context.DemoVisitors.AddAsync(visitor, cancellationToken);
        }
        else if (visitor.LastCodeSentAt is { } sentAt)
        {
            // Arka arkaya istek hem kutuyu doldurur hem de adresi başkasının
            // rahatsız etmesi için bir araca çevirir.
            int wait = (int)Math.Ceiling(
                (sentAt.AddSeconds(_demo.CodeResendSeconds) - now).TotalSeconds);

            if (wait > 0)
                return DemoVerificationResult.Fail(
                    $"Yeni kod istemek için {wait} saniye bekleyin.");
        }

        string code = GenerateCode();

        visitor.DisplayEmail = display;
        visitor.CodeHash = Hash(normalized, code);
        visitor.CodeExpiresAt = now.AddMinutes(_demo.CodeLifetimeMinutes);
        visitor.CodeAttempts = 0;
        visitor.LastCodeSentAt = now;
        visitor.CodesSent++;
        visitor.IpAddress = Trim(ipAddress, 45);
        visitor.UserAgent = Trim(userAgent, 400);

        await context.SaveChangesAsync(cancellationToken);

        try
        {
            await SendMailAsync(display, code, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Demo doğrulama kodu gönderilemedi.");

            // Gönderilemeyen bir kod için ziyaretçiyi bekletmenin anlamı yok;
            // hemen tekrar deneyebilsin diye sayaç geri alınıyor.
            visitor.LastCodeSentAt = null;
            visitor.CodeHash = null;
            await context.SaveChangesAsync(cancellationToken);

            return DemoVerificationResult.Fail(
                "Doğrulama maili gönderilemedi. Birazdan tekrar deneyin.");
        }

        logger.LogInformation("Demo doğrulama kodu gönderildi: {Email}.", normalized);

        return DemoVerificationResult.Ok(
            $"Doğrulama kodu {display} adresine gönderildi. Kod {_demo.CodeLifetimeMinutes} dakika geçerli.");
    }

    private Task SendMailAsync(string email, string code, CancellationToken cancellationToken) =>
        fluentEmail
            .To(email)
            .Subject($"Defter demo doğrulama kodunuz: {code}")
            .Body(
                $"""
                <div style="font-family:Inter,Arial,Helvetica,sans-serif;font-size:15px;color:#0f172a">
                  <h2 style="margin:0 0 12px;font-size:20px">Defter demo kodunuz</h2>
                  <p style="color:#475569">
                    Demoyu başlatmak için aşağıdaki kodu uygulamaya girin.
                  </p>
                  <p style="margin:22px 0">
                    <span style="display:inline-block;background:#eff6ff;border:1px solid #bfdbfe;
                                 border-radius:10px;padding:14px 22px;font-size:28px;font-weight:700;
                                 letter-spacing:.35em;color:#1d4ed8">{code}</span>
                  </p>
                  <p style="color:#64748b;font-size:13px">
                    Kod {_demo.CodeLifetimeMinutes} dakika geçerlidir. Bu isteği siz yapmadıysanız
                    bu maili yok sayabilirsiniz.
                  </p>
                </div>
                """,
                isHtml: true)
            .SendAsync(cancellationToken);

    // --- doğrulama ----------------------------------------------------------

    public async Task<DemoVerificationResult> VerifyAsync(
        string email, string code, CancellationToken cancellationToken = default)
    {
        if (!TryNormalize(email, out string normalized, out _))
            return DemoVerificationResult.Fail("Geçerli bir e-posta adresi yazın.");

        DemoVisitor? visitor = await context.DemoVisitors
            .FirstOrDefaultAsync(p => p.Email == normalized, cancellationToken);

        if (visitor?.CodeHash is null)
            return DemoVerificationResult.Fail("Önce e-posta adresinize kod isteyin.");

        if (visitor.CodeExpiresAt <= DateTimeOffset.Now)
            return DemoVerificationResult.Fail("Kodun süresi doldu. Yeni bir kod isteyin.");

        if (visitor.CodeAttempts >= _demo.MaxCodeAttempts)
            return DemoVerificationResult.Fail("Çok fazla hatalı deneme yaptınız. Yeni bir kod isteyin.");

        string candidate = Hash(normalized, (code ?? string.Empty).Trim());

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(candidate), Encoding.UTF8.GetBytes(visitor.CodeHash)))
        {
            visitor.CodeAttempts++;
            await context.SaveChangesAsync(cancellationToken);

            int left = Math.Max(0, _demo.MaxCodeAttempts - visitor.CodeAttempts);

            return DemoVerificationResult.Fail(left > 0
                ? $"Kod hatalı. {left} deneme hakkınız kaldı."
                : "Kod hatalı. Yeni bir kod isteyin.");
        }

        // Kod tek kullanımlık: doğrulandığı anda tüketiliyor.
        visitor.CodeHash = null;
        visitor.CodeExpiresAt = null;
        visitor.CodeAttempts = 0;
        visitor.VerifiedAt ??= DateTimeOffset.Now;

        await context.SaveChangesAsync(cancellationToken);

        return DemoVerificationResult.Ok("E-posta adresiniz doğrulandı.");
    }

    public async Task RecordSessionAsync(string email, CancellationToken cancellationToken = default)
    {
        if (!TryNormalize(email, out string normalized, out _)) return;

        DemoVisitor? visitor = await context.DemoVisitors
            .FirstOrDefaultAsync(p => p.Email == normalized, cancellationToken);

        if (visitor is null) return;

        visitor.SessionCount++;
        visitor.LastSessionAt = DateTimeOffset.Now;

        await context.SaveChangesAsync(cancellationToken);
    }

    // --- yardımcılar --------------------------------------------------------

    private string GenerateCode()
    {
        int length = Math.Clamp(_demo.CodeLength, 4, 9);
        int max = (int)Math.Pow(10, length);

        return RandomNumberGenerator.GetInt32(0, max).ToString(new string('0', length));
    }

    private static string Hash(string email, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{email}:{code}")));

    /// <summary>
    /// Adresi hem eşleştirilebilir hâle getirir hem de gerçekten bir adres olup
    /// olmadığını doğrular.
    /// </summary>
    private static bool TryNormalize(string? email, out string normalized, out string display)
    {
        normalized = string.Empty;
        display = string.Empty;

        string trimmed = (email ?? string.Empty).Trim();

        if (trimmed.Length is 0 or > 254) return false;
        if (!System.Net.Mail.MailAddress.TryCreate(trimmed, out System.Net.Mail.MailAddress? parsed))
            return false;

        // Nokta içermeyen bir alan adı ("ali@localhost") geçerli sayılır ama posta
        // kutusu değildir.
        if (!parsed!.Host.Contains('.')) return false;

        display = parsed.Address;
        normalized = parsed.Address.ToLowerInvariant();

        return true;
    }

    private static string? Trim(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null
        : value.Length <= max ? value
        : value[..max];
}
