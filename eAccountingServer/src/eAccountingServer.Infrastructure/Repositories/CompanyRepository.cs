using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Infrastructure.Context;
using GenericRepository;

namespace eAccountingServer.Infrastructure.Repositories;
internal class CompanyRepository : Repository<Company, ApplicationDbContext>, ICompanyRepository
{
    public CompanyRepository(ApplicationDbContext context) : base(context)
    {
    }
}
