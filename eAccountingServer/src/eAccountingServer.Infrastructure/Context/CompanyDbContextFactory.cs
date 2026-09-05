using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Design;

namespace eAccountingServer.Infrastructure.Context;

/// <summary>
/// Migration üretirken kullanılır. Firma bağlamı isteğin içinden geliyor,
/// komut satırında ise ortada istek yok; bağlantı dizesi boş kalınca bağlam
/// "firma seçilmedi" diye hata veriyordu. Buradaki adres yalnızca modelin
/// çıkarılabilmesi için var, hiçbir veritabanına bağlanılmıyor.
/// </summary>
internal sealed class CompanyDbContextFactory : IDesignTimeDbContextFactory<CompanyDbContext>
{
    public CompanyDbContext CreateDbContext(string[] args) =>
        new(new Company
        {
            Database = new Database(".", "eAccountingCompanyDesignTime", string.Empty, string.Empty)
        });
}
