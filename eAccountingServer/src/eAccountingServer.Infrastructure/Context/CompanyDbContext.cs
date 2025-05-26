using System.Security.Claims;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace eAccountingServer.Infrastructure.Context;
internal sealed class CompanyDbContext : DbContext, IUnitOfWorkCompany
{
    private string connectionString = string.Empty;

    public CompanyDbContext(Company company)
    {
        CreateConnectionStringWithCompany(company);
    }

    public CompanyDbContext(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
    {
        CreateConnectionString(httpContextAccessor,context);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(connectionString);
    }

    private void CreateConnectionString(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
    {
        if (httpContextAccessor.HttpContext is null) return;

        string? companyId = httpContextAccessor.HttpContext.User.FindFirstValue("CompanyId");
        if (string.IsNullOrEmpty(companyId)) return;

        Company? company = context.Companies.Find(Guid.Parse(companyId));
        if (company is null) return;

        CreateConnectionStringWithCompany(company);
    }

    private void CreateConnectionStringWithCompany(Company company)
    {
        if (string.IsNullOrEmpty(company.Database.Username))
            connectionString = $"" +
            $"Data Source={company.Database.Server};" +
                $"Initial Catalog={company.Database.DatabaseName};" +
                $"Integrated Security=True;" +
                $"Connect Timeout=30;" +
                $"Encrypt=True;" +
                $"Trust Server Certificate=True;" +
                $"Application Intent=ReadWrite;" +
                $"Multi Subnet Failover=False";
        else
            connectionString = $"" +
            $"Data Source={company.Database.Server};" +
                $"Initial Catalog={company.Database.DatabaseName};" +
            $"Integrated Security=False;" +
            $"User ID={company.Database.Username};" +
                $"Password={company.Database.Password}" +
                $"Connect Timeout=30;" +
                $"Encrypt=True;" +
                $"Trust Server Certificate=True;" +
                $"Application Intent=ReadWrite;" +
                $"Multi Subnet Failover=False";
    }
}

