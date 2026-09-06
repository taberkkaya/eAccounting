using eAccountingServer.Domain.Abstractions;
using eAccountingServer.Domain.Enums;

namespace eAccountingServer.Domain.Entities;

/// <summary>
/// Satış ya da alış faturası.
///
/// Toplamlar satırlardan hesaplanıp burada saklanıyor. Her okumada yeniden
/// toplamak daha "doğru" görünürdü ama fatura kesildikten sonra donmuş bir
/// belgedir: ürünün fiyatı sonradan değişince eski fatura değişmemeli.
/// </summary>
public sealed class Invoice : Entity
{
    public InvoiceType Type { get; set; }

    /// <summary>SF2026000001 gibi. Tür ve yıl içinde benzersiz.</summary>
    public string Number { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    /// <summary>Vade. Yaşlandırma ve "gecikmiş" uyarısı buna bakıyor.</summary>
    public DateOnly DueDate { get; set; }

    public Guid ContactId { get; set; }

    public Contact? Contact { get; set; }

    public CurrencyTypeEnum CurrencyType { get; set; } = CurrencyTypeEnum.TL;

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Approved;

    /// <summary>KDV hariç, satır indirimleri düşülmüş toplam.</summary>
    public decimal SubTotal { get; set; }

    public decimal DiscountTotal { get; set; }

    public decimal VatTotal { get; set; }

    /// <summary>Ödenecek tutar: <see cref="SubTotal"/> + <see cref="VatTotal"/>.</summary>
    public decimal GrandTotal { get; set; }

    /// <summary>Bu faturaya karşılık şimdiye kadar tahsil edilen ya da ödenen.</summary>
    public decimal PaidAmount { get; set; }

    public string? Note { get; set; }

    public List<InvoiceLine>? Lines { get; set; }

    public decimal RemainingAmount => GrandTotal - PaidAmount;
}

/// <summary>Faturadaki bir kalem.</summary>
public sealed class InvoiceLine : Entity
{
    public Guid InvoiceId { get; set; }

    /// <summary>Ürün kaydından geldiyse hangisi. Serbest satırlarda boş.</summary>
    public Guid? ProductId { get; set; }

    /// <summary>
    /// Satırın kendi açıklaması. Ürün adının kopyası olarak başlıyor ama ürün
    /// sonradan yeniden adlandırılınca fatura değişmesin diye ayrı duruyor.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    public string Unit { get; set; } = "Adet";

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    /// <summary>Satır indirimi, yüzde.</summary>
    public decimal DiscountRate { get; set; }

    public int VatRate { get; set; }

    /// <summary>İndirim düşülmüş, KDV hariç tutar.</summary>
    public decimal LineTotal { get; set; }

    public decimal VatAmount { get; set; }
}
