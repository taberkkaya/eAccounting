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

        SeedCategories(context);
    }

    /// <summary>
    /// Boş bir kalem listesi işe yaramıyor: ilk hareketi girerken seçecek bir şey
    /// olsun diye yaygın kalemler hazır geliyor. Kullanıcı bunları silebilir ya da
    /// kendi kalemlerini ekleyebilir; sonradan eklenen firmalara karışmasın diye
    /// yalnızca liste bomboşken yazılıyor.
    /// </summary>
    private static void SeedCategories(CompanyDbContext context)
    {
        if (context.Categories.IgnoreQueryFilters().Any()) return;

        (string Name, int Direction)[] defaults =
        [
            ("Satış", 0),
            ("Tahsilat", 0),
            ("Faiz Geliri", 0),
            ("Diğer Gelir", 0),
            ("Kira", 1),
            ("Maaş", 1),
            ("Fatura", 1),
            ("Vergi", 1),
            ("Tedarikçi Ödemesi", 1),
            ("Diğer Gider", 1)
        ];

        context.Categories.AddRange(defaults.Select(item => new Category
        {
            Name = item.Name,
            Direction = item.Direction
        }));

        context.SaveChanges();
    }
}
