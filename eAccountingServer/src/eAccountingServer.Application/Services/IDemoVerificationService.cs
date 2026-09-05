namespace eAccountingServer.Application.Services;

/// <summary>Bir kod isteğinin ya da doğrulamanın sonucu.</summary>
public sealed record DemoVerificationResult(bool Succeeded, string Message)
{
    public static DemoVerificationResult Ok(string message) => new(true, message);

    public static DemoVerificationResult Fail(string message) => new(false, message);
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

    /// <summary>Doğrulanmış ziyaretçinin demo oturumu açtığını işler.</summary>
    Task RecordSessionAsync(string email, CancellationToken cancellationToken = default);
}
