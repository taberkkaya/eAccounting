using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Infrastructure.Context;
using GenericRepository;

namespace eAccountingServer.Infrastructure.Repositories;
internal sealed class CategoryRepository : Repository<Category, CompanyDbContext>, ICategoryRepository
{
    public CategoryRepository(CompanyDbContext context) : base(context)
    {
    }
}
