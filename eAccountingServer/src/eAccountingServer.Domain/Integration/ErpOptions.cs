namespace eAccountingServer.Domain.Integration;

/// <summary>
/// Tezgah (ERP) ile aradaki bağlantı. "Erp" yapılandırma bölümünden bağlanır.
///
/// Kapalıyken Defter tek başına çalışır ve stoğu kendi tutar — tek uygulamayla
/// idare eden bir kurulumun istediği budur. Açıkken stoğun sahibi Tezgah olur:
/// Defter fatura keser, doğan stok hareketini oraya yazar ve eldeki miktarı
/// oradan okur. Aynı işin iki yerde tutulmaması için gereken de bu.
/// </summary>
public sealed class ErpOptions
{
    public const string SectionName = "Erp";

    /// <summary>Kapalıyken hiçbir istek çıkmaz ve stok Defter'de kalır.</summary>
    public bool Enabled { get; set; }

    /// <summary>Tezgah API'sinin kökü, örneğin https://tezgah.example.com.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Tezgah ile paylaşılan gizli anahtar. Boşsa entegrasyon çalışmaz.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Faturadan doğan hareketin işleneceği depo. Tezgah çok depolu, Defter değil;
    /// fatura ekranında depo sorulmadığı için hedef burada sabitleniyor.
    /// </summary>
    public Guid? DefaultDepotId { get; set; }

    /// <summary>
    /// İsteğin bekleyeceği süre. Tezgah yavaşsa fatura kaydı sürüncemede
    /// kalmasın; fatura zaten kaydedilmiş oluyor, bu çağrı onun ardından geliyor.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;
}
