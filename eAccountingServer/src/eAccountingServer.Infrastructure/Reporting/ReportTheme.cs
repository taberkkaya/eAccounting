using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace eAccountingServer.Infrastructure.Reporting;

/// <summary>
/// Rapor çıktılarının ortak görünümü. Ekstre ve hareket listesi ayrı dosyalar
/// üretiyor ama aynı uygulamadan çıkıyorlar; renkler ve alt bilgi tek yerde
/// dursun ki biri değişince diğeri geride kalmasın.
/// </summary>
internal static class ReportTheme
{
    public const string Navy = "#0F172A";
    public const string Blue = "#2563EB";
    public const string Green = "#059669";
    public const string Red = "#DC2626";
    public const string Line = "#E2E8F0";
    public const string Zebra = "#F8FAFC";
    public const string Muted = "#64748B";
    public const string Wash = "#EFF6FF";

    /// <summary>QuestPDF topluluk lisansı; bireysel ve küçük ölçekli kullanım için.</summary>
    public static void EnsureLicense() =>
        QuestPDF.Settings.License = LicenseType.Community;

    /// <summary>Tutarlar Türkçe biçimde, binlik ayracıyla ve sembolle yazılır.</summary>
    public static string Money(decimal amount, string symbol) =>
        string.Create(
            System.Globalization.CultureInfo.GetCultureInfo("tr-TR"),
            $"{amount:N2} {symbol}").Trim();

    public static void ComposeFooter(IContainer container)
    {
        container.BorderTop(0.5f).BorderColor(Line).PaddingTop(7).Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.Span("Defter").FontSize(7.5f).SemiBold().FontColor(Muted);
                text.Span($"  ·  {DateTime.Now:dd.MM.yyyy HH:mm} tarihinde oluşturuldu")
                    .FontSize(7.5f).FontColor(Muted);
            });

            row.ConstantItem(90).AlignRight().Text(text =>
            {
                text.DefaultTextStyle(style => style.FontSize(7.5f).FontColor(Muted));
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }
}
