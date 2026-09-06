using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Enums;
using eAccountingServer.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.Contacts;

public sealed record ContactStatementLineDto(
    Guid Id,
    DateOnly Date,
    string Description,
    int Kind,
    string KindName,
    decimal DebitAmount,
    decimal CreditAmount,
    /// <summary>Satırdan sonraki bakiye; ekstre bunun için okunuyor.</summary>
    decimal RunningBalance,
    Guid? InvoiceId,
    string? InvoiceNumber,
    Guid? AccountId,
    string? AccountName);

public sealed record ContactStatementDto(
    Guid ContactId,
    string ContactName,
    string CurrencyName,
    DateOnly? StartDate,
    DateOnly? EndDate,
    /// <summary>Aralıktan önceki bakiye. Aralık dışı hareketler yutulmasın diye.</summary>
    decimal OpeningBalance,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal ClosingBalance,
    List<ContactStatementLineDto> Lines);

public sealed record GetContactStatementQuery(
    Guid ContactId,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null) : IRequest<Result<ContactStatementDto>>;

internal sealed class GetContactStatementQueryHandler(
    IContactRepository contactRepository,
    IContactTransactionRepository contactTransactionRepository,
    IInvoiceRepository invoiceRepository,
    ICashRegisterRepository cashRegisterRepository,
    IBankRepository bankRepository
    ) : IRequestHandler<GetContactStatementQuery, Result<ContactStatementDto>>
{
    public async Task<Result<ContactStatementDto>> Handle(
        GetContactStatementQuery request, CancellationToken cancellationToken)
    {
        Contact? contact = await contactRepository
            .Where(p => p.Id == request.ContactId).FirstOrDefaultAsync(cancellationToken);

        if (contact is null)
            return Result<ContactStatementDto>.Failure("Cari bulunamadı.");

        List<ContactTransaction> all = await contactTransactionRepository
            .Where(p => p.ContactId == request.ContactId)
            .OrderBy(p => p.Date).ThenBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        // Aralıktan öncesi tek satıra iniyor: "devir" olmadan ekstre kendi
        // içinde tutarsız görünür, kullanıcı aradaki farkı arar.
        decimal opening = request.StartDate is { } start
            ? all.Where(p => p.Date < start).Sum(p => p.DebitAmount - p.CreditAmount)
            : 0;

        List<ContactTransaction> lines = all
            .Where(p => request.StartDate is not { } s || p.Date >= s)
            .Where(p => request.EndDate is not { } e || p.Date <= e)
            .ToList();

        Dictionary<Guid, string> invoiceNumbers = await InvoiceNumbersAsync(
            lines, cancellationToken);

        Dictionary<Guid, string> accountNames = await AccountNamesAsync(lines, cancellationToken);

        decimal running = opening;
        List<ContactStatementLineDto> result = [];

        foreach (ContactTransaction line in lines)
        {
            running += line.DebitAmount - line.CreditAmount;

            result.Add(new ContactStatementLineDto(
                line.Id, line.Date, line.Description,
                (int)line.Kind, KindName(line.Kind),
                line.DebitAmount, line.CreditAmount, running,
                line.InvoiceId,
                line.InvoiceId is { } invoiceId
                    && invoiceNumbers.TryGetValue(invoiceId, out string? number) ? number : null,
                line.AccountId,
                line.AccountId is { } accountId
                    && accountNames.TryGetValue(accountId, out string? name) ? name : null));
        }

        return new ContactStatementDto(
            contact.Id, contact.Name, contact.CurrencyType.Name,
            request.StartDate, request.EndDate,
            opening,
            lines.Sum(p => p.DebitAmount),
            lines.Sum(p => p.CreditAmount),
            running,
            result);
    }

    private async Task<Dictionary<Guid, string>> InvoiceNumbersAsync(
        List<ContactTransaction> lines, CancellationToken cancellationToken)
    {
        List<Guid> ids = lines
            .Where(p => p.InvoiceId is not null)
            .Select(p => p.InvoiceId!.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0) return [];

        return await invoiceRepository
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Number, cancellationToken);
    }

    private async Task<Dictionary<Guid, string>> AccountNamesAsync(
        List<ContactTransaction> lines, CancellationToken cancellationToken)
    {
        List<Guid> ids = lines
            .Where(p => p.AccountId is not null)
            .Select(p => p.AccountId!.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0) return [];

        Dictionary<Guid, string> names = await cashRegisterRepository
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        foreach (KeyValuePair<Guid, string> bank in await bankRepository
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken))
            names[bank.Key] = bank.Value;

        return names;
    }

    internal static string KindName(ContactTransactionKind kind) => kind switch
    {
        ContactTransactionKind.Opening => "Açılış",
        ContactTransactionKind.Invoice => "Fatura",
        ContactTransactionKind.Collection => "Tahsilat",
        ContactTransactionKind.Payment => "Ödeme",
        _ => "Düzeltme"
    };
}
