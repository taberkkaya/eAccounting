using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Infrastructure.Context;
using GenericRepository;

namespace eAccountingServer.Infrastructure.Repositories;

internal sealed class InvoiceLineRepository : Repository<InvoiceLine, CompanyDbContext>, IInvoiceLineRepository
{
    public InvoiceLineRepository(CompanyDbContext context) : base(context)
    {
    }
}
