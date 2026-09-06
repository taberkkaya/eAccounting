using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Infrastructure.Context;
using GenericRepository;

namespace eAccountingServer.Infrastructure.Repositories;

internal sealed class InvoiceRepository : Repository<Invoice, CompanyDbContext>, IInvoiceRepository
{
    public InvoiceRepository(CompanyDbContext context) : base(context)
    {
    }
}
