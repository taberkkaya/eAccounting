namespace eAccountingServer.Domain.Mail;

/// <summary>"Mail" yapılandırma bölümünden bağlanır.</summary>
public sealed class MailOptions
{
    public const string SectionName = "Mail";

    /// <summary>
    /// Gönderen adresi. Alıcı sunucuların SPF/DKIM doğrulaması yapabilmesi için
    /// gerçekten sahip olunan bir alan adı olmalı.
    /// </summary>
    public string From { get; set; } = string.Empty;

    public string? FromName { get; set; }

    /// <summary>Boş bırakılırsa mailler gönderilmez, sessizce düşürülür.</summary>
    public string? SmtpHost { get; set; }

    public int SmtpPort { get; set; } = 587;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// Onay bağlantısının kurulacağı adres. Mail alıcının makinesinde değil,
    /// uygulamanın yayınlandığı adreste açılmalı.
    /// </summary>
    public string ClientBaseUrl { get; set; } = "http://localhost:4200";
}
