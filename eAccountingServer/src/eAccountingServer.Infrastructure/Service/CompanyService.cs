using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace eAccountingServer.Infrastructure.Service;
internal class CompanyService : ICompanyService
{
    public void MigrateAllCompanies(List<Company> companies)
    {
        foreach(var company in companies)
        {
            CompanyDbContext context = new(company);

            context.Database.Migrate();
        }
    }
}
