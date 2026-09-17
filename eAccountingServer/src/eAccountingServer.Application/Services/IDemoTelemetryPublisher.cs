namespace eAccountingServer.Application.Services;

/// <summary>
/// Demo kullanımını ataberkkaya.com paneline bildirir.
///
/// Bütün üyeler ateşle-unut: çağıran beklemez ve hata görmez. Bu bilinçli —
/// istatistik kaydı, ziyaretçinin oturumunu açmasının ya da kapatmasının önünde
/// duracak kadar önemli değil.
/// </summary>
public interface IDemoTelemetryPublisher
{
    /// <summary>Oturum açıldı. Ziyaretçinin adresi bu noktada henüz bilinmiyor olabilir.</summary>
    void SessionStarted(Guid sessionId, DateTimeOffset startedAt);

    /// <summary>
    /// Oturumu açan ziyaretçi belli oldu. Ayrı bir çağrı, çünkü adres doğrulaması
    /// oturumu açan uçta, oturumun kendisi havuzda yönetiliyor. Panel aynı oturum
    /// kimliğini gördüğü için kayıt ikiye bölünmüyor, üzerine yazılıyor.
    /// </summary>
    void VisitorIdentified(Guid sessionId, string email, string? country, string? city);

    void SessionEnded(Guid sessionId, string reason, int writesUsed, int writeLimit);

    /// <summary>
    /// Uygulamanın ayakta olduğunu ve havuzun doluluğunu bildirir. Panel
    /// "çevrimdışı" yargısını son nabza bakarak veriyor.
    /// </summary>
    void Heartbeat(bool demoEnabled, int slotCount, int busySlots);
}
