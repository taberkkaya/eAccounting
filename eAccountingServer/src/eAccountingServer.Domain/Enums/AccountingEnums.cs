namespace eAccountingServer.Domain.Enums;

/// <summary>
/// Bir carinin ne olduğu. Aynı firma hem müşteri hem tedarikçi olabildiği için
/// üçüncü bir seçenek var; kayıt ikiye bölünmesin.
/// </summary>
public enum ContactType
{
    Customer = 1,
    Supplier = 2,
    Both = 3
}

/// <summary>Satış mı alış mı: faturanın yönü buradan çıkıyor.</summary>
public enum InvoiceType
{
    Sales = 1,
    Purchase = 2
}

/// <summary>
/// Faturanın hayatındaki yer. Taslak hiçbir yere işlemez; onaylanınca cariye ve
/// stoğa yazılır, tahsilat girildikçe kısmen ya da tamamen kapanır.
/// </summary>
public enum InvoiceStatus
{
    Draft = 0,
    Approved = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Cancelled = 4
}

/// <summary>
/// Cari hareketin nereden geldiği. Ekstrede satırın yanında gösteriliyor ve
/// elle silinemeyecek satırları (faturadan gelenler) ayırmaya yarıyor.
/// </summary>
public enum ContactTransactionKind
{
    Opening = 0,
    Invoice = 1,
    Collection = 2,
    Payment = 3,
    Adjustment = 4
}

/// <summary>Stok hareketinin yönü.</summary>
public enum StockDirection
{
    In = 0,
    Out = 1
}

/// <summary>Paranın hangi hesap türünde durduğu.</summary>
public enum AccountKind
{
    CashRegister = 0,
    Bank = 1
}
