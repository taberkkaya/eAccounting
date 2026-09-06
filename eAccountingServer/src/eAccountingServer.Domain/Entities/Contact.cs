using eAccountingServer.Domain.Abstractions;
using eAccountingServer.Domain.Enums;

namespace eAccountingServer.Domain.Entities;

/// <summary>
/// Cari hesap: bir müşteri ya da tedarikçi.
///
/// Kasa ve banka "param nerede" sorusunu cevaplıyor; cari "kim bana borçlu, ben
/// kime borçluyum" sorusunu. Ön muhasebeyi kasa defterinden ayıran şey bu.
///
/// Bakiye <see cref="CashRegister"/> ile aynı mantıkta tutuluyor: hareketler
/// tek tek toplanmıyor, iki koşu toplamı satırda duruyor.
/// </summary>
public sealed class Contact : Entity
{
    public string Name { get; set; } = string.Empty;

    public ContactType Type { get; set; } = ContactType.Customer;

    /// <summary>Vergi kimlik ya da TC kimlik numarası. Zorunlu değil.</summary>
    public string? TaxNumber { get; set; }

    public string? TaxOffice { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    /// <summary>Serbest not; "ödemeleri hep geç yapıyor" gibi.</summary>
    public string? Note { get; set; }

    /// <summary>
    /// Carinin çalıştığı para birimi. Fatura ve tahsilatlar bununla eşleşmek
    /// zorunda: kur çevirisi yapmadan farklı birimleri toplamak bakiyeyi
    /// anlamsız kılardı.
    /// </summary>
    public CurrencyTypeEnum CurrencyType { get; set; } = CurrencyTypeEnum.TL;

    /// <summary>Borç toplamı: cariye kestiğimiz faturalar ve ona yaptığımız ödemeler.</summary>
    public decimal DebitAmount { get; set; }

    /// <summary>Alacak toplamı: ondan aldığımız tahsilatlar ve bize kestiği faturalar.</summary>
    public decimal CreditAmount { get; set; }

    public List<ContactTransaction>? Transactions { get; set; }

    /// <summary>Artı ise cari bize borçlu, eksi ise biz ona borçluyuz.</summary>
    public decimal Balance => DebitAmount - CreditAmount;
}
