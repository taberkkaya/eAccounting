namespace eAccountingServer.Domain.Mail;

/// <summary>
/// Giden maillerin ortak iskeleti. Tek yerde durması, iki farklı mailin zamanla
/// birbirinden ayrı düşmesini engelliyor.
///
/// Düzen tablolarla kuruluyor ve her stil satır içinde: Outlook ve benzeri
/// istemciler harici stil sayfalarını, flexbox'ı ve grid'i uygulamıyor.
/// </summary>
public static class MailTemplate
{
    private const string Ink = "#0f172a";
    private const string Body = "#475569";
    private const string Muted = "#94a3b8";
    private const string Brand = "#2563eb";
    private const string Line = "#e2e8f0";
    private const string Page = "#f1f5f9";

    private const string Font =
        "-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif";

    /// <summary>
    /// Başlık bandı, beyaz kart ve alt bilgiden oluşan gövdeyi kurar.
    /// <paramref name="content"/> kartın içine gelen hazır HTML'dir.
    /// </summary>
    public static string Wrap(string heading, string content, string siteUrl)
    {
        string host = Host(siteUrl);

        return $"""
            <!DOCTYPE html>
            <html lang="tr">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <title>{heading}</title>
            </head>
            <body style="margin:0;padding:0;background:{Page};">
              <!-- Önizleme satırı: gelen kutusunda konu başlığının yanında görünür. -->
              <div style="display:none;max-height:0;overflow:hidden;opacity:0;">{heading}</div>

              <table role="presentation" width="100%" cellpadding="0" cellspacing="0"
                     style="background:{Page};padding:32px 12px;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="600" cellpadding="0" cellspacing="0"
                           style="width:100%;max-width:600px;border-collapse:collapse;">

                      <!-- başlık bandı -->
                      <tr>
                        <td style="background:{Ink};border-radius:12px 12px 0 0;padding:22px 32px;">
                          <table role="presentation" cellpadding="0" cellspacing="0">
                            <tr>
                              <td style="background:{Brand};border-radius:8px;width:34px;height:34px;
                                         text-align:center;vertical-align:middle;color:#ffffff;
                                         font-family:{Font};font-size:17px;font-weight:700;">D</td>
                              <td style="padding-left:12px;font-family:{Font};">
                                <div style="color:#ffffff;font-size:16px;font-weight:700;line-height:1.2;">Defter</div>
                                <div style="color:{Muted};font-size:12px;line-height:1.4;">ön muhasebe</div>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      <!-- gövde -->
                      <tr>
                        <td style="background:#ffffff;border-left:1px solid {Line};
                                   border-right:1px solid {Line};padding:32px;font-family:{Font};">
                          <h1 style="margin:0 0 18px;color:{Ink};font-size:20px;
                                     font-weight:700;line-height:1.3;">{heading}</h1>
                          {content}
                        </td>
                      </tr>

                      <!-- alt bilgi -->
                      <tr>
                        <td style="background:#ffffff;border:1px solid {Line};border-top:0;
                                   border-radius:0 0 12px 12px;padding:20px 32px;
                                   font-family:{Font};font-size:12px;color:{Muted};line-height:1.6;">
                          Bu mail <a href="{siteUrl}" style="color:{Brand};text-decoration:none;">{host}</a>
                          üzerinden gönderildi. Yanıtlamanıza gerek yok.
                        </td>
                      </tr>

                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    /// <summary>Gövde metni.</summary>
    public static string Paragraph(string html) =>
        $"""<p style="margin:0 0 14px;color:{Body};font-size:15px;line-height:1.65;">{html}</p>""";

    /// <summary>
    /// Eylem düğmesi. Outlook yuvarlak köşeli bağlantıyı boyamadığı için düğme
    /// tek hücreli bir tablo olarak kuruluyor.
    /// </summary>
    public static string Button(string text, string url) =>
        $"""
        <table role="presentation" cellpadding="0" cellspacing="0" style="margin:24px 0;">
          <tr>
            <td style="background:{Brand};border-radius:8px;">
              <a href="{url}"
                 style="display:inline-block;padding:13px 26px;color:#ffffff;font-family:{Font};
                        font-size:15px;font-weight:600;text-decoration:none;">{text}</a>
            </td>
          </tr>
        </table>
        """;

    /// <summary>Girilmek üzere gösterilen tek kullanımlık kod.</summary>
    public static string Code(string code) =>
        $"""
        <table role="presentation" cellpadding="0" cellspacing="0" style="margin:24px 0;">
          <tr>
            <td style="background:#eff6ff;border:1px solid #bfdbfe;border-radius:10px;
                       padding:16px 26px;font-family:{Font};font-size:30px;font-weight:700;
                       letter-spacing:8px;color:#1d4ed8;">{code}</td>
          </tr>
        </table>
        """;

    /// <summary>
    /// Düğmeye basamayanlar için ham adres. Uzun jetonlar satırı taşırdığı için
    /// küçük punto ve kırılabilir bir kutuda duruyor.
    /// </summary>
    public static string FallbackLink(string url) =>
        $"""
        <p style="margin:20px 0 0;color:{Muted};font-size:12px;line-height:1.5;">
          Düğme çalışmazsa bu adresi tarayıcınıza yapıştırın:
        </p>
        <p style="margin:6px 0 0;padding:10px 12px;background:{Page};border-radius:6px;
                  color:{Body};font-size:11px;line-height:1.5;word-break:break-all;">{url}</p>
        """;

    /// <summary>Alt kısımdaki küçük açıklama.</summary>
    public static string Note(string text) =>
        $"""
        <p style="margin:22px 0 0;padding-top:18px;border-top:1px solid {Line};
                  color:{Muted};font-size:12.5px;line-height:1.6;">{text}</p>
        """;

    private static string Host(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? parsed) ? parsed.Host : url;
}
