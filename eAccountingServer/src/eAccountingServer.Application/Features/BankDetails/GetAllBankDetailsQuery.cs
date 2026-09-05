using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.BankDetails;
public sealed record GetAllBankDetailsQuery(
    Guid BankId,
    DateOnly StartDate,
    DateOnly EndDate
    ) : IRequest<Result<Bank>>;

internal sealed class GetAllBankDetailsHandler(
    IBankRepository bankRepository
    ) : IRequestHandler<GetAllBankDetailsQuery, Result<Bank>>
{
    public async Task<Result<Bank>> Handle(GetAllBankDetailsQuery request, CancellationToken cancellationToken)
    {
       Bank? cashRegister = await bankRepository
      .Where(p => p.Id == request.BankId)
      .Include(p => p.Details!
          .Where(p =>
              p.Date >= request.StartDate
              && p.Date <= request.EndDate))
      .FirstOrDefaultAsync(cancellationToken);

        if (cashRegister is null)
            return Result<Bank>.Failure("Banka bulunamadı.");

        return cashRegister;
    }
}
