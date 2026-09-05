using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.CashRegisterDetails;
public sealed record GetAllCashRegisterDetailsQuery(
    Guid CashRegisterId,
    DateOnly StartDate,
    DateOnly EndDate
    ) : IRequest<Result<CashRegister>>;

internal sealed class GetAllCashRegisterDetailsQueryHandler(
    ICashRegisterRepository cashRegisterRepository
    ) : IRequestHandler<GetAllCashRegisterDetailsQuery, Result<CashRegister>>
{
    public async Task<Result<CashRegister>> Handle(GetAllCashRegisterDetailsQuery request, CancellationToken cancellationToken)
    {
        CashRegister? cashRegister = await cashRegisterRepository
        .Where(p => p.Id == request.CashRegisterId)
        .Include(p => p.Details!
            .Where(p =>
                p.Date >= request.StartDate
                && p.Date <= request.EndDate))
        .FirstOrDefaultAsync(cancellationToken);

        if (cashRegister is null)
            return Result<CashRegister>.Failure("Kasa bulunamadı.");

        return cashRegister;
    }
}