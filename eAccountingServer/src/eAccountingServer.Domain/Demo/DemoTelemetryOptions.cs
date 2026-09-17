namespace eAccountingServer.Domain.Demo;

/// <summary>
/// Demo kullanımının ataberkkaya.com paneline bildirilmesi. "DemoTelemetry"
/// yapılandırma bölümünden bağlanır.
///
/// Bildirim tek yönlü ve ateşle-unut: panel erişilemezse demo çalışmaya devam
/// eder, yalnızca o olay kaydedilmez. Ziyaretçinin oturumu bir istatistik
/// kaydının başarısına bağlanmamalı.
/// </summary>
public sealed class DemoTelemetryOptions
{
    public const string SectionName = "DemoTelemetry";

    /// <summary>Kapalıyken hiçbir istek çıkmaz.</summary>
    public bool Enabled { get; set; }

    /// <summary>Panelin API kökü, örneğin https://ataberkkaya.com.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Panelle paylaşılan gizli anahtar. Boşsa bildirim gönderilmez.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Panelde bu uygulamayı tanıtan anahtar.</summary>
    public string AppKey { get; set; } = "defter";

    public string AppName { get; set; } = "Defter";

    /// <summary>Demonun yayındaki adresi; panel bunu bağlantı olarak gösteriyor.</summary>
    public string AppUrl { get; set; } = string.Empty;

    /// <summary>
    /// İsteğin bekleyeceği süre. Kısa tutuluyor: panel yavaşsa demo oturumunun
    /// açılması gecikmemeli.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;
}
