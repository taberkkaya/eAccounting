using eAccountingServer.Domain.Abstractions;
using eAccountingServer.Domain.Enums;

namespace eAccountingServer.Domain.Entities;

/// <summary>
/// Bir ürünün stoğundaki tek bir değişiklik.
///
/// <see cref="Product.StockQuantity"/> bu satırların özeti. İkisini birlikte
/// tutmak, "eldeki miktar" sorusunu her seferinde tabloyu tarayarak cevaplamak
/// zorunda kalmadan, "bu miktar nereden geldi" sorusunu da cevaplayabilmek için.
/// </summary>
public sealed class StockTransaction : Entity
{
    public Guid ProductId { get; set; }

    public DateOnly Date { get; set; }

    public StockDirection Direction { get; set; }

    public decimal Quantity { get; set; }

    /// <summary>Hareket anındaki birim fiyat; maliyet raporu buna bakıyor.</summary>
    public decimal UnitPrice { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>Faturadan geldiyse hangi fatura.</summary>
    public Guid? InvoiceId { get; set; }
}
