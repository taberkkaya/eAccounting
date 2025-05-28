using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Infrastructure.Context;
using GenericRepository;

namespace eAccountingServer.Infrastructure.Repositories;
internal sealed class CashRegisterDetailRepository : Repository<CashRegisterDetail, CompanyDbContext>, ICashRegisterDetailRepository
{
    public CashRegisterDetailRepository(CompanyDbContext context) : base(context)
    {
    }
}
