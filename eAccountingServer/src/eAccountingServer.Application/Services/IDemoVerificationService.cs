using eAccountingServer.Domain.Demo;

namespace eAccountingServer.Application.Services;

/// <summary>Panele bildirilecek kadarıyla ziyaretçi.</summary>
public sealed record DemoVisitorSnapshot(string Email, string? Country, string? City);

/// <summary>Bir kod isteğinin ya da doğrulamanın sonucu.</summary>
/// <param name="AlreadyVerified">
/// Kod gönderilmedi çünkü gerekmiyor: ziyaretçi yakın zamanda doğrulanmış. İstemci
/// kod adımını atlayıp doğrudan demoyu başlatır.
/// </param>
public sealed record DemoVerificationResult(bool Succeeded, string Message, bool AlreadyVerified = false)
{
    public static DemoVerificationResult Ok(string message) => new(true, message);

    public static DemoVerificationResult Fail(string message) => new(false, message);

    /// <summary>Doğrulanmış ziyaretçi; kod adımına hiç girilmeyecek.</summary>
    public static DemoVerificationResult Skip(string message) => new(true, message, true);
}

/// <summary>
/// Demoya girmeden önce ziyaretçinin e-posta adresini doğrular ve kimin ne zaman
/// denediğini kaydeder.
/// </summary>
public interface IDemoVerificationService
{
    /// <summary>
    /// Doğrulamanın gerçekten uygulanıp uygulanmadığı. Ayar açık olsa bile mail
    /// gönderilemiyorsa false döner: kod ulaşmayacağı için ziyaretçiyi kapıda
    /// bırakmanın anlamı yok.
    /// </summary>
    bool Required { get; }

    Task<DemoVerificationResult> SendCodeAsync(
        string email, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);

    Task<DemoVerificationResult> VerifyAsync(
        string email, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ziyaretçi <see cref="DemoOptions.VerifiedGraceHours"/> içinde doğrulanmış mı.
    /// Doğrulanmışsa kod istemeden yeni oturum açabilir: oturumunu kendi kapatan biri
    /// geri döndüğünde kapıda bekletilmemeli.
    /// </summary>
    Task<bool> HasValidVerificationAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Doğrulanmış ziyaretçinin demo oturumu açtığını işler ve bilinen konumunu
    /// döner. Konum, oturumu panele bildirirken kullanılıyor: ziyaretçi kaydı
    /// burada, oturum kimliği ise havuzda olduğu için ikisini çağıran birleştiriyor.
    /// </summary>
    Task<DemoVisitorSnapshot?> RecordSessionAsync(string email, CancellationToken cancellationToken = default);
}
