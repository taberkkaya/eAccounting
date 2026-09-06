using eAccountingServer.Domain.Abstractions;
using eAccountingServer.Domain.Enums;

namespace eAccountingServer.Domain.Entities;

/// <summary>
/// Cari ekstresindeki bir satır.
///
/// Kimse bu satırları doğrudan yazmıyor: fatura kesilince, tahsilat girilince ya
/// da açılış bakiyesi verilince kaynak işlem bir tane üretiyor. Kaynağı silinen
/// satır da siliniyor; ekstrenin faturayla çelişmemesi buna bağlı.
/// </summary>
public sealed class ContactTransaction : Entity
{
    public Guid ContactId { get; set; }

    public DateOnly Date { get; set; }

    public string Description { get; set; } = string.Empty;

    public ContactTransactionKind Kind { get; set; }

    /// <summary>Carinin borcunu artıran tutar.</summary>
    public decimal DebitAmount { get; set; }

    /// <summary>Carinin borcunu azaltan tutar.</summary>
    public decimal CreditAmount { get; set; }

    /// <summary>Faturadan geldiyse hangi fatura. Elle girilen satırlarda boş.</summary>
    public Guid? InvoiceId { get; set; }

    /// <summary>
    /// Tahsilat/ödeme ise paranın hangi kasa ya da bankaya gittiği. Karşı
    /// taraftaki hareket silinince bu da silinsin diye tutuluyor.
    /// </summary>
    public AccountKind? AccountKind { get; set; }

    public Guid? AccountId { get; set; }

    /// <summary>Kasa ya da banka tarafındaki hareketin kimliği.</summary>
    public Guid? AccountTransactionId { get; set; }
}
