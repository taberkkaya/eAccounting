using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.CashRegisters;
public sealed record GetAllCashRegistersQuery() : IRequest<Result<List<CashRegister>>>;

internal sealed class GetAllCashRegistersQueryHandler(
    ICashRegisterRepository cashRegisterRepository,
    ICacheService cacheService
    ) : IRequestHandler<GetAllCashRegistersQuery, Result<List<CashRegister>>>
{
    public async Task<Result<List<CashRegister>>> Handle(GetAllCashRegistersQuery request, CancellationToken cancellationToken)
    {
        List<CashRegister>? cashRegisters;
        
        cashRegisters = cacheService.Get<List<CashRegister>>("cashRegisters");

        if(cashRegisters is null)
        {
            cashRegisters = await cashRegisterRepository
                .GetAll()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);

            cacheService.Set<List<CashRegister>>("cashRegisters", cashRegisters);
        }

        return cashRegisters;
    }
}
