using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Infrastructure.Context;
using GenericRepository;

namespace eAccountingServer.Infrastructure.Repositories;
internal sealed class CashRegisterRepository : Repository<CashRegister, CompanyDbContext>, ICashRegisterRepository
{
    public CashRegisterRepository(CompanyDbContext context) : base(context)
    {
    }
}
