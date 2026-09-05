using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace eAccountingServer.Infrastructure.Service;
internal class CompanyService : ICompanyService
{
    public void MigrateAllCompanies(List<Company> companies)
    {
        foreach (var company in companies)
        {
            MigrateCompany(company);
        }
    }

    public void MigrateCompany(Company company)
    {
        using CompanyDbContext context = new(company);

        // Migrate veritabanı yoksa onu da oluşturuyor; sunucuda bu yetki varsa
        // firma tek adımda kullanılabilir hâle geliyor.
        context.Database.Migrate();
    }
}
