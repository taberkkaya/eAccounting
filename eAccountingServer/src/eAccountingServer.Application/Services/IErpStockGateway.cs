namespace eAccountingServer.Application.Services;

/// <summary>Tezgah'a gönderilecek tek bir stok satırı.</summary>
public sealed record ErpStockLine(Guid ErpProductId, decimal Quantity, decimal UnitPrice);

/// <summary>Tezgah'taki bir ürün; fatura satırını eşlerken kullanılıyor.</summary>
public sealed record ErpProduct(Guid Id, string Name, string ProductType, decimal Stock);

public sealed record ErpDepot(Guid Id, string Name);

/// <summary>Bir çağrının sonucu. Başarısızlık faturayı geri almıyor, yalnızca bildiriliyor.</summary>
public sealed record ErpResult(bool Succeeded, string? Message)
{
    public static readonly ErpResult Skipped = new(true, null);

    public static ErpResult Ok() => new(true, null);

    public static ErpResult Fail(string message) => new(false, message);
}

/// <summary>
/// Tezgah'ın stok tarafıyla konuşur.
///
/// Fatura Defter'de kesiliyor ama stoğun sahibi Tezgah; onaylanan bir fatura
/// orada bir stok hareketine dönüşüyor. Çağrılar faturanın kaydından sonra
/// yapılıyor: Tezgah'a ulaşılamaması geçerli bir faturanın kaydedilmesini
/// engellememeli, o yüzden hata sonuçta bildiriliyor, istisna atılmıyor.
/// </summary>
public interface IErpStockGateway
{
    /// <summary>Yapılandırma eksikse false; çağıran o zaman stoğu kendi tutar.</summary>
    bool Enabled { get; }

    /// <summary>
    /// Faturanın doğurduğu hareketi yazar. Aynı fatura yeniden gönderilebilir:
    /// Tezgah eski hareketleri silip yenilerini yazıyor.
    /// </summary>
    Task<ErpResult> PushInvoiceAsync(
        Guid invoiceId,
        string invoiceNumber,
        bool isPurchase,
        IReadOnlyList<ErpStockLine> lines,
        CancellationToken cancellationToken = default);

    /// <summary>Fatura silindiğinde onun doğurduğu hareketleri de kaldırır.</summary>
    Task<ErpResult> RemoveInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    /// <summary>Tezgah'taki ürünler; Defter'in ürün kartını eşlerken gösteriliyor.</summary>
    Task<IReadOnlyList<ErpProduct>> GetProductsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ErpDepot>> GetDepotsAsync(CancellationToken cancellationToken = default);
}
