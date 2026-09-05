using eAccountingServer.Domain.Entities;

namespace eAccountingServer.Application.Services;
public interface ICompanyService
{
    void MigrateAllCompanies(List<Company> companies);

    /// <summary>
    /// Firmanın veritabanını kurar; yoksa oluşturur. Firma kaydedilmeden önce
    /// çağrılıyor, böylece kurulum başarısız olursa ortada yarım bir firma kalmıyor.
    /// </summary>
    void MigrateCompany(Company company);
}
