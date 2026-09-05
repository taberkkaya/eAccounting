using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.Movements;

/// <summary>
/// Bütün kasa ve banka hesaplarının son hareketleri, tek listede.
/// </summary>
public sealed record GetRecentMovementsQuery(int Take = 10) : IRequest<Result<List<MovementDto>>>;

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
    bool IsTransfer);

internal sealed class GetRecentMovementsQueryHandler(
    ICashRegisterRepository cashRegisterRepository,
    ICashRegisterDetailRepository cashRegisterDetailRepository,
    IBankRepository bankRepository,
    IBankDetailRepository bankDetailRepository
    ) : IRequestHandler<GetRecentMovementsQuery, Result<List<MovementDto>>>
{
    public async Task<Result<List<MovementDto>>> Handle(
        GetRecentMovementsQuery request, CancellationToken cancellationToken)
    {
        int take = Math.Clamp(request.Take, 1, 100);

        // Hareket tabloları kendi taraflarında sıralanıp sınırlanıyor, hesap adı
        // ve para birimi ise hesap listesinden ekleniyor. Tek sorguda birleştirmek
        // mümkün değil: para birimi veritabanında sayı olarak duruyor ve adı
        // ancak nesne belleğe alınınca ortaya çıkıyor.
        List<CashRegister> cashAccounts = await cashRegisterRepository
            .GetAll().ToListAsync(cancellationToken);

        List<Bank> bankAccounts = await bankRepository
            .GetAll().ToListAsync(cancellationToken);

        List<CashRegisterDetail> cashDetails = await cashRegisterDetailRepository
            .GetAll()
            .OrderByDescending(detail => detail.Date)
            .ThenByDescending(detail => detail.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        List<BankDetail> bankDetails = await bankDetailRepository
            .GetAll()
            .OrderByDescending(detail => detail.Date)
            .ThenByDescending(detail => detail.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, CashRegister> cashById = cashAccounts.ToDictionary(a => a.Id);
        Dictionary<Guid, Bank> bankById = bankAccounts.ToDictionary(a => a.Id);

        IEnumerable<MovementDto> cash = cashDetails
            .Where(detail => cashById.ContainsKey(detail.CashRegisterId))
            .Select(detail =>
            {
                CashRegister account = cashById[detail.CashRegisterId];

                return new MovementDto(
                    detail.Id,
                    account.Id,
                    account.Name,
                    "Kasa",
                    account.CurrencyType.Name,
                    detail.Date,
                    detail.Description,
                    detail.DepositAmount,
                    detail.WithdrawalAmount,
                    detail.CashRegisterDetailId is not null);
            });

        IEnumerable<MovementDto> bank = bankDetails
            .Where(detail => bankById.ContainsKey(detail.BankId))
            .Select(detail =>
            {
                Bank account = bankById[detail.BankId];

                return new MovementDto(
                    detail.Id,
                    account.Id,
                    account.Name,
                    "Banka",
                    account.CurrencyType.Name,
                    detail.Date,
                    detail.Description,
                    detail.DepositAmount,
                    detail.WithdrawalAmount,
                    detail.BankDetailId is not null);
            });

        return cash
            .Concat(bank)
            .OrderByDescending(movement => movement.Date)
            .Take(take)
            .ToList();
    }
}
