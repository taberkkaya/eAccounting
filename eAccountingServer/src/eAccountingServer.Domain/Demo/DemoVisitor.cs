using eAccountingServer.Domain.Abstractions;

namespace eAccountingServer.Domain.Demo;

/// <summary>
/// Demoyu denemek için e-posta adresini doğrulayan ziyaretçi. Kayıt ana
/// veritabanında tutulur: sandbox veritabanları her kiralamada sıfırlandığı için
/// oraya yazılan hiçbir şey kalıcı olmaz.
///
/// Adres başına tek satır; aynı kişi tekrar geldiğinde sayaçları büyür.
/// </summary>
public sealed class DemoVisitor : Entity
{
    /// <summary>Küçük harfe indirgenmiş adres; eşleştirme bunun üzerinden yapılır.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Ziyaretçinin yazdığı hâli, yalnızca gösterim için.</summary>
    public string DisplayEmail { get; set; } = string.Empty;

    /// <summary>
    /// Bekleyen kodun karması. Kodun kendisi saklanmaz; veritabanını görebilen
    /// birinin geçerli bir kodu okuyabilmesi için sebep yok.
    /// </summary>
    public string? CodeHash { get; set; }

    public DateTimeOffset? CodeExpiresAt { get; set; }

    /// <summary>Bekleyen kod için yapılan başarısız deneme sayısı.</summary>
    public int CodeAttempts { get; set; }

    /// <summary>Arka arkaya kod isteğini sınırlamak için.</summary>
    public DateTimeOffset? LastCodeSentAt { get; set; }

    public int CodesSent { get; set; }

    public DateTimeOffset? VerifiedAt { get; set; }

    public bool IsVerified => VerifiedAt is not null;

    public int SessionCount { get; set; }

    public DateTimeOffset? LastSessionAt { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
}
