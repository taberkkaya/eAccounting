using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Infrastructure.Context;
using GenericRepository;

namespace eAccountingServer.Infrastructure.Repositories;

internal sealed class ContactTransactionRepository : Repository<ContactTransaction, CompanyDbContext>, IContactTransactionRepository
{
    public ContactTransactionRepository(CompanyDbContext context) : base(context)
    {
    }
}
