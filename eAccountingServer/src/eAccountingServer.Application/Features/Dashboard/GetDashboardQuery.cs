using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Enums;
using eAccountingServer.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.Dashboard;

/// <summary>Para birimi başına bir tutar; farklı birimler toplanmıyor.</summary>
public sealed record CurrencyAmountDto(string CurrencyName, decimal Amount);

public sealed record DueInvoiceDto(
    Guid Id,
    string Number,
    int Type,
    Guid ContactId,
    string ContactName,
    DateOnly DueDate,
    decimal RemainingAmount,
    string CurrencyName,
    /// <summary>Eksi ise vade geçmiş.</summary>
    int DaysLeft);

public sealed record ContactBalanceDto(
    Guid Id, string Name, string CurrencyName, decimal Balance);

public sealed record LowStockDto(
    Guid Id, string Name, string Unit, decimal StockQuantity, decimal CriticalStock);

/// <summary>
/// Ana sayfanın tek çağrıda ihtiyacı olan her şey.
///
/// Ayrı ayrı uçlardan toplamak istemciyi altı isteğe zorluyordu ve toplamlar
/// istemcide hesaplandığı için ekranla rapor arasında fark çıkabiliyordu.
/// </summary>
public sealed record DashboardDto(
    List<CurrencyAmountDto> CashBalances,
    List<CurrencyAmountDto> Receivables,
    List<CurrencyAmountDto> Payables,
    List<CurrencyAmountDto> OverdueReceivables,
    List<CurrencyAmountDto> OverduePayables,
    List<CurrencyAmountDto> MonthSales,
    List<CurrencyAmountDto> MonthPurchases,
    List<DueInvoiceDto> UpcomingInvoices,
    List<ContactBalanceDto> TopDebtors,
    List<ContactBalanceDto> TopCreditors,
    List<LowStockDto> LowStock,
    int ContactCount,
    int ProductCount,
    int OpenInvoiceCount);

public sealed record GetDashboardQuery(int UpcomingDays = 30) : IRequest<Result<DashboardDto>>;

internal sealed class GetDashboardQueryHandler(
    ICashRegisterRepository cashRegisterRepository,
    IBankRepository bankRepository,
    IContactRepository contactRepository,
    IInvoiceRepository invoiceRepository,
    IProductRepository productRepository
    ) : IRequestHandler<GetDashboardQuery, Result<DashboardDto>>
{
    public async Task<Result<DashboardDto>> Handle(
        GetDashboardQuery request, CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly monthStart = new(today.Year, today.Month, 1);

        List<CashRegister> registers = await cashRegisterRepository
            .GetAll().ToListAsync(cancellationToken);

        List<Bank> banks = await bankRepository.GetAll().ToListAsync(cancellationToken);
        List<Contact> contacts = await contactRepository.GetAll().ToListAsync(cancellationToken);
        List<Invoice> invoices = await invoiceRepository.GetAll().ToListAsync(cancellationToken);
        List<Product> products = await productRepository.GetAll().ToListAsync(cancellationToken);

        List<CurrencyAmountDto> cash = registers
            .Select(p => (p.CurrencyType.Name, Amount: p.DepositAmount - p.WithdrawalAmount))
            .Concat(banks.Select(p => (p.CurrencyType.Name, Amount: p.DepositAmount - p.WithdrawalAmount)))
            .GroupBy(p => p.Name)
            .Select(g => new CurrencyAmountDto(g.Key, g.Sum(p => p.Amount)))
            .OrderBy(p => p.CurrencyName)
            .ToList();

        // Bakiyesi artı olan cari bize borçlu (alacağımız), eksi olan bizden
        // alacaklı (borcumuz). İkisini tek toplamda birleştirmek ikisini de gizler.
        List<CurrencyAmountDto> receivables = ByCurrency(
            contacts.Where(p => p.Balance > 0), p => p.Balance);

        List<CurrencyAmountDto> payables = ByCurrency(
            contacts.Where(p => p.Balance < 0), p => -p.Balance);

        Dictionary<Guid, Contact> contactsById = contacts.ToDictionary(p => p.Id);

        List<Invoice> openInvoices = invoices
            .Where(p => p.Status != InvoiceStatus.Paid)
            .ToList();

        List<Invoice> overdue = openInvoices.Where(p => p.DueDate < today).ToList();

        return new DashboardDto(
            cash,
            receivables,
            payables,
            OverdueBy(overdue, InvoiceType.Sales),
            OverdueBy(overdue, InvoiceType.Purchase),
            ByCurrency(
                invoices.Where(p => p.Type == InvoiceType.Sales && p.Date >= monthStart),
                p => p.GrandTotal),
            ByCurrency(
                invoices.Where(p => p.Type == InvoiceType.Purchase && p.Date >= monthStart),
                p => p.GrandTotal),
            openInvoices
                .Where(p => p.DueDate <= today.AddDays(request.UpcomingDays))
                .OrderBy(p => p.DueDate)
                .Take(8)
                .Select(p => new DueInvoiceDto(
                    p.Id, p.Number, (int)p.Type, p.ContactId,
                    contactsById.TryGetValue(p.ContactId, out Contact? c) ? c.Name : "—",
                    p.DueDate, p.GrandTotal - p.PaidAmount, p.CurrencyType.Name,
                    p.DueDate.DayNumber - today.DayNumber))
                .ToList(),
            contacts.Where(p => p.Balance > 0)
                .OrderByDescending(p => p.Balance).Take(5)
                .Select(p => new ContactBalanceDto(p.Id, p.Name, p.CurrencyType.Name, p.Balance))
                .ToList(),
            contacts.Where(p => p.Balance < 0)
                .OrderBy(p => p.Balance).Take(5)
                .Select(p => new ContactBalanceDto(p.Id, p.Name, p.CurrencyType.Name, -p.Balance))
                .ToList(),
            products
                .Where(p => !p.IsService && p.CriticalStock > 0 && p.StockQuantity <= p.CriticalStock)
                .OrderBy(p => p.StockQuantity)
                .Take(8)
                .Select(p => new LowStockDto(
                    p.Id, p.Name, p.Unit, p.StockQuantity, p.CriticalStock))
                .ToList(),
            contacts.Count,
            products.Count,
            openInvoices.Count);

        List<CurrencyAmountDto> OverdueBy(List<Invoice> source, InvoiceType type) =>
            source
                .Where(p => p.Type == type)
                .GroupBy(p => p.CurrencyType.Name)
                .Select(g => new CurrencyAmountDto(g.Key, g.Sum(p => p.GrandTotal - p.PaidAmount)))
                .OrderBy(p => p.CurrencyName)
                .ToList();
    }

    private static List<CurrencyAmountDto> ByCurrency<T>(
        IEnumerable<T> source, Func<T, decimal> amount) where T : class
    {
        return source
            .GroupBy(CurrencyOf)
            .Select(g => new CurrencyAmountDto(g.Key, g.Sum(amount)))
            .OrderBy(p => p.CurrencyName)
            .ToList();

        static string CurrencyOf(T item) => item switch
        {
            Contact contact => contact.CurrencyType.Name,
            Invoice invoice => invoice.CurrencyType.Name,
            _ => "TL"
        };
    }
}
