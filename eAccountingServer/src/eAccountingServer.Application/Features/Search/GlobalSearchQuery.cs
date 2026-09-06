using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Enums;
using eAccountingServer.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.Search;

/// <param name="Kind">contact, product, invoice, account — istemci ikonu ve adresi buradan seçiyor.</param>
/// <param name="Hint">İkinci satır: vergi numarası, kod, cari adı gibi ayırt edici bilgi.</param>
/// <param name="Meta">Sağ tarafta duran kısa bilgi: bakiye, stok, tutar.</param>
public sealed record SearchHitDto(
    string Kind,
    Guid Id,
    string Title,
    string? Hint,
    string? Meta);

public sealed record GlobalSearchResultDto(List<SearchHitDto> Hits);

/// <summary>
/// Bütün kayıtlarda tek arama.
///
/// Uygulama cari, ürün ve fatura ekranlarına bölünmüş durumda; bir ismi
/// aramak için önce hangi ekranda olduğuna karar vermek gerekiyordu. Burası
/// o kararı ortadan kaldırıyor.
/// </summary>
public sealed record GlobalSearchQuery(string Term, int Take = 12)
    : IRequest<Result<GlobalSearchResultDto>>;

internal sealed class GlobalSearchQueryHandler(
    IContactRepository contactRepository,
    IProductRepository productRepository,
    IInvoiceRepository invoiceRepository,
    ICashRegisterRepository cashRegisterRepository,
    IBankRepository bankRepository
    ) : IRequestHandler<GlobalSearchQuery, Result<GlobalSearchResultDto>>
{
    public async Task<Result<GlobalSearchResultDto>> Handle(
        GlobalSearchQuery request, CancellationToken cancellationToken)
    {
        string term = (request.Term ?? string.Empty).Trim();

        // Tek harfte bütün tabloları taramanın anlamı yok; kullanıcı henüz ne
        // aradığını yazmamış oluyor.
        if (term.Length < 2)
            return new GlobalSearchResultDto([]);

        int take = Math.Clamp(request.Take, 1, 30);
        List<SearchHitDto> hits = [];

        foreach (Contact contact in await contactRepository.GetAll().ToListAsync(cancellationToken))
        {
            if (!Matches(term, contact.Name, contact.TaxNumber, contact.Phone, contact.Email))
                continue;

            hits.Add(new SearchHitDto(
                "contact", contact.Id, contact.Name,
                contact.TaxNumber is null ? ContactKind(contact) : $"{ContactKind(contact)} · {contact.TaxNumber}",
                Money(contact.DebitAmount - contact.CreditAmount, contact.CurrencyType.Name)));
        }

        foreach (Product product in await productRepository.GetAll().ToListAsync(cancellationToken))
        {
            if (!Matches(term, product.Name, product.Code)) continue;

            hits.Add(new SearchHitDto(
                "product", product.Id, product.Name,
                product.Code is null
                    ? (product.IsService ? "Hizmet" : "Ürün")
                    : $"{(product.IsService ? "Hizmet" : "Ürün")} · {product.Code}",
                product.IsService
                    ? Money(product.SalePrice, product.CurrencyType.Name)
                    : $"{product.StockQuantity:0.###} {product.Unit}"));
        }

        Dictionary<Guid, string> contactNames = await contactRepository
            .GetAll().ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        foreach (Invoice invoice in await invoiceRepository
            .GetAll().OrderByDescending(p => p.Date).Take(500).ToListAsync(cancellationToken))
        {
            string contactName = contactNames.TryGetValue(invoice.ContactId, out string? name)
                ? name : string.Empty;

            if (!Matches(term, invoice.Number, contactName, invoice.Note)) continue;

            hits.Add(new SearchHitDto(
                "invoice", invoice.Id, invoice.Number,
                $"{(invoice.Type == InvoiceType.Sales ? "Satış" : "Alış")} · {contactName}",
                Money(invoice.GrandTotal, invoice.CurrencyType.Name)));
        }

        foreach (CashRegister register in await cashRegisterRepository
            .GetAll().ToListAsync(cancellationToken))
        {
            if (!Matches(term, register.Name)) continue;

            hits.Add(new SearchHitDto(
                "cash", register.Id, register.Name, "Kasa",
                Money(register.DepositAmount - register.WithdrawalAmount, register.CurrencyType.Name)));
        }

        foreach (Bank bank in await bankRepository.GetAll().ToListAsync(cancellationToken))
        {
            if (!Matches(term, bank.Name, bank.IBAN)) continue;

            hits.Add(new SearchHitDto(
                "bank", bank.Id, bank.Name, "Banka",
                Money(bank.DepositAmount - bank.WithdrawalAmount, bank.CurrencyType.Name)));
        }

        return new GlobalSearchResultDto([.. hits.Take(take)]);
    }

    private static string ContactKind(Contact contact) => contact.Type switch
    {
        ContactType.Customer => "Müşteri",
        ContactType.Supplier => "Tedarikçi",
        _ => "Müşteri / Tedarikçi"
    };

    /// <summary>
    /// Aksansız ve büyük-küçük harf duyarsız karşılaştırır: "sirket" yazan
    /// "Şirket"i bulabilmeli, Türkçede aksansız yazmak yaygın.
    /// </summary>
    private static bool Matches(string term, params string?[] fields)
    {
        string needle = Simplify(term);

        return fields.Any(field =>
            !string.IsNullOrWhiteSpace(field) && Simplify(field).Contains(needle));
    }

    private static string Simplify(string value)
    {
        Span<char> buffer = stackalloc char[value.Length];
        int length = 0;

        foreach (char c in value.ToLowerInvariant())
            buffer[length++] = c switch
            {
                'ç' => 'c',
                'ğ' => 'g',
                'ı' => 'i',
                'i' => 'i',
                'ö' => 'o',
                'ş' => 's',
                'ü' => 'u',
                _ => c
            };

        return new string(buffer[..length]);
    }

    private static string Money(decimal amount, string currency)
    {
        string symbol = currency switch
        {
            "TL" => "₺",
            "USD" => "$",
            "EURO" or "EUR" => "€",
            _ => string.Empty
        };

        return string.Create(
            System.Globalization.CultureInfo.GetCultureInfo("tr-TR"),
            $"{amount:N2} {symbol}").Trim();
    }
}
