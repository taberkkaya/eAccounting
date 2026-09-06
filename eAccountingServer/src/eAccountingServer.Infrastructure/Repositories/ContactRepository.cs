using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Infrastructure.Context;
using GenericRepository;

namespace eAccountingServer.Infrastructure.Repositories;

internal sealed class ContactRepository : Repository<Contact, CompanyDbContext>, IContactRepository
{
    public ContactRepository(CompanyDbContext context) : base(context)
    {
    }
}
