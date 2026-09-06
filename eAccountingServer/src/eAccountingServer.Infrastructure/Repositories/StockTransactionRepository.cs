using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Infrastructure.Context;
using GenericRepository;

namespace eAccountingServer.Infrastructure.Repositories;

internal sealed class StockTransactionRepository : Repository<StockTransaction, CompanyDbContext>, IStockTransactionRepository
{
    public StockTransactionRepository(CompanyDbContext context) : base(context)
    {
    }
}
