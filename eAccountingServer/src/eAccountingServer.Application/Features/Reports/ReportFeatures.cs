using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Enums;
using eAccountingServer.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.Reports;

// --- yaşlandırma -------------------------------------------------------------

/// <summary>
/// Bir carinin açık bakiyesinin vade kovalarına dağılımı.
///
/// "Ne kadar alacağım var" sorusunun cevabı tek başına yetmiyor; parasının ne
/// kadarının 90 günü geçtiğini bilmeyen bir işletme tahsilat yapamaz.
/// </summary>
public sealed record AgingRowDto(
    Guid ContactId,
    string ContactName,
    string CurrencyName,
    decimal NotDue,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Over90,
    decimal Total);

public sealed record AgingReportDto(
    int Type,
    string TypeName,
    DateOnly AsOf,
    List<AgingRowDto> Rows,
    List<AgingRowDto> Totals);

/// <param name="Type">1 satış (alacaklarımız), 2 alış (borçlarımız).</param>
public sealed record GetAgingReportQuery(int Type = 1) : IRequest<Result<AgingReportDto>>;

internal sealed class GetAgingReportQueryHandler(
    IInvoiceRepository invoiceRepository,
    IContactRepository contactRepository
    ) : IRequestHandler<GetAgingReportQuery, Result<AgingReportDto>>
{
    public async Task<Result<AgingReportDto>> Handle(
        GetAgingReportQuery request, CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        InvoiceType type = (InvoiceType)request.Type;

        List<Invoice> invoices = await invoiceRepository
            .Where(p => p.Type == type && p.Status != InvoiceStatus.Paid)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, Contact> contacts = await contactRepository
            .GetAll().ToDictionaryAsync(p => p.Id, cancellationToken);

        List<AgingRowDto> rows = invoices
            .GroupBy(p => p.ContactId)
            .Select(group =>
            {
                string name = contacts.TryGetValue(group.Key, out Contact? contact)
                    ? contact.Name : "—";

                string currency = group.First().CurrencyType.Name;

                decimal Bucket(Func<int, bool> matches) => group
                    .Where(p => matches(today.DayNumber - p.DueDate.DayNumber))
                    .Sum(p => p.GrandTotal - p.PaidAmount);

                decimal notDue = Bucket(days => days <= 0);
                decimal d30 = Bucket(days => days is > 0 and <= 30);
                decimal d60 = Bucket(days => days is > 30 and <= 60);
                decimal d90 = Bucket(days => days is > 60 and <= 90);
                decimal over = Bucket(days => days > 90);

                return new AgingRowDto(group.Key, name, currency,
                    notDue, d30, d60, d90, over, notDue + d30 + d60 + d90 + over);
            })
            .Where(p => p.Total != 0)
            .OrderByDescending(p => p.Over90).ThenByDescending(p => p.Total)
            .ToList();

        // Toplam satırı para birimi başına: farklı birimleri toplamak yanlış olur.
        List<AgingRowDto> totals = rows
            .GroupBy(p => p.CurrencyName)
            .Select(g => new AgingRowDto(
                Guid.Empty, "TOPLAM", g.Key,
                g.Sum(p => p.NotDue), g.Sum(p => p.Days1To30), g.Sum(p => p.Days31To60),
                g.Sum(p => p.Days61To90), g.Sum(p => p.Over90), g.Sum(p => p.Total)))
            .OrderBy(p => p.CurrencyName)
            .ToList();

        return new AgingReportDto(
            request.Type,
            type == InvoiceType.Sales ? "Alacak Yaşlandırma" : "Borç Yaşlandırma",
            today, rows, totals);
    }
}

// --- KDV ---------------------------------------------------------------------

public sealed record VatRateRowDto(int Rate, decimal Base, decimal Vat);

/// <summary>
/// Dönemin KDV özeti. Beyanname değil; muhasebeciye giderken elde ne olduğunu
/// gösteren özet.
/// </summary>
public sealed record VatReportDto(
    DateOnly StartDate,
    DateOnly EndDate,
    string CurrencyName,
    /// <summary>Satış faturalarından hesaplanan KDV.</summary>
    List<VatRateRowDto> Collected,
    /// <summary>Alış faturalarından indirilecek KDV.</summary>
    List<VatRateRowDto> Deductible,
    decimal CollectedTotal,
    decimal DeductibleTotal,
    /// <summary>Artı ise ödenecek, eksi ise devreden KDV.</summary>
    decimal Payable);

public sealed record GetVatReportQuery(DateOnly StartDate, DateOnly EndDate)
    : IRequest<Result<VatReportDto>>;

internal sealed class GetVatReportQueryHandler(
    IInvoiceRepository invoiceRepository,
    IInvoiceLineRepository invoiceLineRepository
    ) : IRequestHandler<GetVatReportQuery, Result<VatReportDto>>
{
    public async Task<Result<VatReportDto>> Handle(
        GetVatReportQuery request, CancellationToken cancellationToken)
    {
        // KDV yalnızca TL üzerinden anlamlı; dövizli faturalar kur çevirisi
        // gerektirir ve o çeviri burada yapılmıyor.
        List<Invoice> invoices = await invoiceRepository
            .Where(p => p.Date >= request.StartDate && p.Date <= request.EndDate)
            .ToListAsync(cancellationToken);

        invoices = invoices.Where(p => p.CurrencyType.Value == CurrencyTypeEnum.TL.Value).ToList();

        List<Guid> ids = invoices.Select(p => p.Id).ToList();

        List<InvoiceLine> lines = ids.Count == 0
            ? []
            : await invoiceLineRepository
                .Where(p => ids.Contains(p.InvoiceId)).ToListAsync(cancellationToken);

        Dictionary<Guid, InvoiceType> typeById = invoices.ToDictionary(p => p.Id, p => p.Type);

        List<VatRateRowDto> Rows(InvoiceType type) => lines
            .Where(p => typeById.TryGetValue(p.InvoiceId, out InvoiceType t) && t == type)
            .GroupBy(p => p.VatRate)
            .Select(g => new VatRateRowDto(g.Key, g.Sum(p => p.LineTotal), g.Sum(p => p.VatAmount)))
            .OrderBy(p => p.Rate)
            .ToList();

        List<VatRateRowDto> collected = Rows(InvoiceType.Sales);
        List<VatRateRowDto> deductible = Rows(InvoiceType.Purchase);

        decimal collectedTotal = collected.Sum(p => p.Vat);
        decimal deductibleTotal = deductible.Sum(p => p.Vat);

        return new VatReportDto(
            request.StartDate, request.EndDate, CurrencyTypeEnum.TL.Name,
            collected, deductible, collectedTotal, deductibleTotal,
            collectedTotal - deductibleTotal);
    }
}

// --- kâr / zarar -------------------------------------------------------------

public sealed record ProfitLossDto(
    DateOnly StartDate,
    DateOnly EndDate,
    string CurrencyName,
    /// <summary>Satış faturaları, KDV hariç.</summary>
    decimal Revenue,
    /// <summary>Alış faturaları, KDV hariç.</summary>
    decimal Cost,
    /// <summary>Faturasız kasa/banka giderleri; kalemlere göre dağılım.</summary>
    decimal OtherExpenses,
    decimal Profit,
    List<CategoryTotalDto> ExpenseByCategory,
    List<MonthlyTotalDto> Monthly);

public sealed record CategoryTotalDto(string Name, decimal Amount);

public sealed record MonthlyTotalDto(int Year, int Month, decimal Revenue, decimal Cost);

public sealed record GetProfitLossQuery(DateOnly StartDate, DateOnly EndDate)
    : IRequest<Result<ProfitLossDto>>;

internal sealed class GetProfitLossQueryHandler(
    IInvoiceRepository invoiceRepository,
    ICashRegisterDetailRepository cashRegisterDetailRepository,
    IBankDetailRepository bankDetailRepository,
    ICategoryRepository categoryRepository
    ) : IRequestHandler<GetProfitLossQuery, Result<ProfitLossDto>>
{
    public async Task<Result<ProfitLossDto>> Handle(
        GetProfitLossQuery request, CancellationToken cancellationToken)
    {
        List<Invoice> invoices = (await invoiceRepository
            .Where(p => p.Date >= request.StartDate && p.Date <= request.EndDate)
            .ToListAsync(cancellationToken))
            .Where(p => p.CurrencyType.Value == CurrencyTypeEnum.TL.Value)
            .ToList();

        decimal revenue = invoices.Where(p => p.Type == InvoiceType.Sales).Sum(p => p.SubTotal);
        decimal cost = invoices.Where(p => p.Type == InvoiceType.Purchase).Sum(p => p.SubTotal);

        // Cariye bağlı olmayan çıkışlar: kira, maaş, fatura... Bunlar faturaya
        // girmediği için satış/alış toplamlarında görünmüyor.
        List<(Guid? CategoryId, decimal Amount)> expenses =
        [
            .. (await cashRegisterDetailRepository
                .Where(p => p.Date >= request.StartDate
                    && p.Date <= request.EndDate
                    && p.WithdrawalAmount > 0
                    && p.ContactId == null
                    && p.CashRegisterDetailId == null)
                .Select(p => new { p.CategoryId, Amount = p.WithdrawalAmount })
                .ToListAsync(cancellationToken))
                .Select(p => (p.CategoryId, p.Amount)),

            .. (await bankDetailRepository
                .Where(p => p.Date >= request.StartDate
                    && p.Date <= request.EndDate
                    && p.WithdrawalAmount > 0
                    && p.ContactId == null
                    && p.BankDetailId == null)
                .Select(p => new { p.CategoryId, Amount = p.WithdrawalAmount })
                .ToListAsync(cancellationToken))
                .Select(p => (p.CategoryId, p.Amount))
        ];

        Dictionary<Guid, string> categories = await categoryRepository
            .GetAll().ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        List<CategoryTotalDto> byCategory = expenses
            .GroupBy(p => p.CategoryId is { } id && categories.TryGetValue(id, out string? name)
                ? name : "Kalemsiz")
            .Select(g => new CategoryTotalDto(g.Key, g.Sum(p => p.Amount)))
            .OrderByDescending(p => p.Amount)
            .ToList();

        decimal otherExpenses = expenses.Sum(p => p.Amount);

        List<MonthlyTotalDto> monthly = invoices
            .GroupBy(p => new { p.Date.Year, p.Date.Month })
            .Select(g => new MonthlyTotalDto(
                g.Key.Year, g.Key.Month,
                g.Where(p => p.Type == InvoiceType.Sales).Sum(p => p.SubTotal),
                g.Where(p => p.Type == InvoiceType.Purchase).Sum(p => p.SubTotal)))
            .OrderBy(p => p.Year).ThenBy(p => p.Month)
            .ToList();

        return new ProfitLossDto(
            request.StartDate, request.EndDate, CurrencyTypeEnum.TL.Name,
            revenue, cost, otherExpenses,
            revenue - cost - otherExpenses,
            byCategory, monthly);
    }
}
