using eAccountingServer.Domain.Abstractions;
using eAccountingServer.Domain.Enums;

namespace eAccountingServer.Domain.Entities;

/// <summary>
/// Satılan ya da alınan bir şey: ürün veya hizmet.
///
/// Hizmetin stoğu yok, o yüzden <see cref="IsService"/> yalnızca bir etiket
/// değil; stok hareketinin yazılıp yazılmayacağını belirliyor.
/// </summary>
public sealed class Product : Entity
{
    /// <summary>Kısa kod ya da barkod. Zorunlu değil ama varsa benzersiz.</summary>
    public string? Code { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Adet, kg, saat, paket... serbest metin; birim listesi dayatmıyoruz.</summary>
    public string Unit { get; set; } = "Adet";

    public bool IsService { get; set; }

    public decimal PurchasePrice { get; set; }

    public decimal SalePrice { get; set; }

    /// <summary>KDV yüzdesi: 0, 1, 10, 20.</summary>
    public int VatRate { get; set; } = 20;

    public CurrencyTypeEnum CurrencyType { get; set; } = CurrencyTypeEnum.TL;

    /// <summary>Eldeki miktar. Hizmetlerde her zaman sıfır.</summary>
    public decimal StockQuantity { get; set; }

    /// <summary>
    /// Bu miktarın altına düşünce ana sayfada uyarılır. Sıfır ise uyarı yok.
    /// </summary>
    public decimal CriticalStock { get; set; }

    public string? Description { get; set; }
}
