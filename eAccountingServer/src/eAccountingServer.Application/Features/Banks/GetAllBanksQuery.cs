using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.Banks;
public sealed record GetAllBanksQuery : IRequest<Result<List<Bank>>>;

internal sealed class GetAllBanksQueryHandler(
    IBankRepository bankRepository,
    ICacheService cacheService
    ) : IRequestHandler<GetAllBanksQuery, Result<List<Bank>>>
{
    public async Task<Result<List<Bank>>> Handle(GetAllBanksQuery request, CancellationToken cancellationToken)
    {
        List<Bank>? banks;

        banks = cacheService.Get<List<Bank>>("banks");

        if (banks is null)
        {
            banks = await bankRepository
                            .GetAll()
                            .OrderBy(p => p.Name)
                            .ToListAsync(cancellationToken);

            cacheService.Set("banks",banks);
        }

        return banks;
    }
}
