using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Infrastructure.Context;
using GenericRepository;

namespace eAccountingServer.Infrastructure.Repositories;
internal class BankRepository : Repository<Bank, CompanyDbContext>, IBankRepository
{
    public BankRepository(CompanyDbContext context) : base(context)
    {
    }
}
