using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Infrastructure.Context;
using GenericRepository;

namespace eAccountingServer.Infrastructure.Repositories;
internal sealed class CompanyUserRepository : Repository<CompanyUser, ApplicationDbContext>, ICompanyUserRepository
{
    public CompanyUserRepository(ApplicationDbContext context) : base(context)
    {
    }
}