using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.Movements;

public sealed record MovementDto(
    Guid Id,
    Guid AccountId,
    string AccountName,
    /// <summary>"Kasa" ya da "Banka"; istemci hangi sayfaya gideceğini buradan biliyor.</summary>
    string AccountKind,
    string CurrencyName,
    DateOnly Date,
    string Description,
    decimal Deposit,
    decimal Withdrawal,
    bool IsTransfer,
    Guid? CategoryId,
    string? CategoryName);

/// <summary>Ana sayfadaki kısa liste.</summary>
public sealed record GetRecentMovementsQuery(int Take = 10) : IRequest<Result<List<MovementDto>>>;

/// <summary>
/// Bütün hesapların hareketleri, filtreli. Kasa ve banka ayrımı burada yok:
/// para hareketi para hareketidir, hangi hesapta durduğu yalnızca bir sütun.
/// </summary>
public sealed record GetMovementsQuery(
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    /// <summary>0 giriş, 1 çıkış, null ikisi de.</summary>
    int? Direction = null,
    Guid? AccountId = null,
    Guid? CategoryId = null,
    string? Search = null,
    int Take = 500) : IRequest<Result<List<MovementDto>>>;

internal sealed class MovementReader(
    ICashRegisterRepository cashRegisterRepository,
    ICashRegisterDetailRepository cashRegisterDetailRepository,
    IBankRepository bankRepository,
    IBankDetailRepository bankDetailRepository,
    ICategoryRepository categoryRepository)
{
    /// <summary>
    /// İki hareket tablosunu tek listeye indirger. Hesap adı ve para birimi
    /// hesaplardan, kalem adı kalemlerden ekleniyor: para birimi veritabanında
    /// sayı olarak durduğu için adı ancak nesne belleğe alınınca ortaya çıkıyor,
    /// bu yüzden birleştirme SQL'de değil burada yapılıyor.
    /// </summary>
    public async Task<List<MovementDto>> ReadAsync(
        GetMovementsQuery filter, CancellationToken cancellationToken)
    {
        int take = Math.Clamp(filter.Take, 1, 2000);

        Dictionary<Guid, CashRegister> cashAccounts = (await cashRegisterRepository
            .GetAll().ToListAsync(cancellationToken)).ToDictionary(a => a.Id);

        Dictionary<Guid, Bank> bankAccounts = (await bankRepository
            .GetAll().ToListAsync(cancellationToken)).ToDictionary(a => a.Id);

        Dictionary<Guid, string> categories = (await categoryRepository
            .GetAll().ToListAsync(cancellationToken)).ToDictionary(c => c.Id, c => c.Name);

        List<MovementDto> movements = [];

        bool wantsCash = filter.AccountId is null || cashAccounts.ContainsKey(filter.AccountId.Value);
        bool wantsBank = filter.AccountId is null || bankAccounts.ContainsKey(filter.AccountId.Value);

        if (wantsCash)
        {
            IQueryable<CashRegisterDetail> query = cashRegisterDetailRepository.GetAll();

            if (filter.StartDate is { } start) query = query.Where(d => d.Date >= start);
            if (filter.EndDate is { } end) query = query.Where(d => d.Date <= end);
            if (filter.Direction == 0) query = query.Where(d => d.DepositAmount > 0);
            if (filter.Direction == 1) query = query.Where(d => d.WithdrawalAmount > 0);
            if (filter.CategoryId is { } category) query = query.Where(d => d.CategoryId == category);
            if (filter.AccountId is { } account) query = query.Where(d => d.CashRegisterId == account);

            List<CashRegisterDetail> details = await query
                .OrderByDescending(detail => detail.Date)
                .ThenByDescending(detail => detail.CreatedAt)
                .Take(take)
                .ToListAsync(cancellationToken);

            movements.AddRange(details
                .Where(detail => cashAccounts.ContainsKey(detail.CashRegisterId))
                .Select(detail => Map(
                    detail.Id, cashAccounts[detail.CashRegisterId].Id,
                    cashAccounts[detail.CashRegisterId].Name, "Kasa",
                    cashAccounts[detail.CashRegisterId].CurrencyType.Name,
                    detail.Date, detail.Description, detail.DepositAmount, detail.WithdrawalAmount,
                    detail.CashRegisterDetailId is not null, detail.CategoryId, categories)));
        }

        if (wantsBank)
        {
            IQueryable<BankDetail> query = bankDetailRepository.GetAll();

            if (filter.StartDate is { } start) query = query.Where(d => d.Date >= start);
            if (filter.EndDate is { } end) query = query.Where(d => d.Date <= end);
            if (filter.Direction == 0) query = query.Where(d => d.DepositAmount > 0);
            if (filter.Direction == 1) query = query.Where(d => d.WithdrawalAmount > 0);
            if (filter.CategoryId is { } category) query = query.Where(d => d.CategoryId == category);
            if (filter.AccountId is { } account) query = query.Where(d => d.BankId == account);

            List<BankDetail> details = await query
                .OrderByDescending(detail => detail.Date)
                .ThenByDescending(detail => detail.CreatedAt)
                .Take(take)
                .ToListAsync(cancellationToken);

            movements.AddRange(details
                .Where(detail => bankAccounts.ContainsKey(detail.BankId))
                .Select(detail => Map(
                    detail.Id, bankAccounts[detail.BankId].Id,
                    bankAccounts[detail.BankId].Name, "Banka",
                    bankAccounts[detail.BankId].CurrencyType.Name,
                    detail.Date, detail.Description, detail.DepositAmount, detail.WithdrawalAmount,
                    detail.BankDetailId is not null, detail.CategoryId, categories)));
        }

        IEnumerable<MovementDto> result = movements;

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string term = filter.Search.Trim();

            result = result.Where(movement =>
                movement.Description.Contains(term, StringComparison.OrdinalIgnoreCase)
                || movement.AccountName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (movement.CategoryName ?? string.Empty)
                    .Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return result
            .OrderByDescending(movement => movement.Date)
            .Take(take)
            .ToList();
    }

    private static MovementDto Map(
        Guid id, Guid accountId, string accountName, string kind, string currency,
        DateOnly date, string description, decimal deposit, decimal withdrawal,
        bool isTransfer, Guid? categoryId, Dictionary<Guid, string> categories) =>
        new(id, accountId, accountName, kind, currency, date, description,
            deposit, withdrawal, isTransfer, categoryId,
            categoryId is { } key && categories.TryGetValue(key, out string? name) ? name : null);
}

internal sealed class GetRecentMovementsQueryHandler(
    MovementReader reader
    ) : IRequestHandler<GetRecentMovementsQuery, Result<List<MovementDto>>>
{
    public async Task<Result<List<MovementDto>>> Handle(
        GetRecentMovementsQuery request, CancellationToken cancellationToken) =>
        await reader.ReadAsync(new GetMovementsQuery(Take: request.Take), cancellationToken);
}

internal sealed class GetMovementsQueryHandler(
    MovementReader reader
    ) : IRequestHandler<GetMovementsQuery, Result<List<MovementDto>>>
{
    public async Task<Result<List<MovementDto>>> Handle(
        GetMovementsQuery request, CancellationToken cancellationToken) =>
        await reader.ReadAsync(request, cancellationToken);
}
