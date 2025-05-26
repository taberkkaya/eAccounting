using eAccountingServer.Domain.Entities;

namespace eAccountingServer.Application.Services;
public interface ICompanyService
{
    void MigrateAllCompanies(List<Company> companies);
}
