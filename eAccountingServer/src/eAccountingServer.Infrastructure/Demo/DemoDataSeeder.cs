using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Enums;
using eAccountingServer.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace eAccountingServer.Infrastructure.Demo;

/// <summary>
/// Fills a sandbox database with a small but plausible set of books so a visitor lands
/// on a populated screen instead of an empty grid. The script is fixed rather than
/// random: every visitor should see the same starting position.
/// </summary>
internal static class DemoDataSeeder
{
    private sealed record Movement(
        int DaysAgo, string Description, decimal Deposit, decimal Withdrawal, string Category);

    public static async Task ResetAsync(CompanyDbContext context, Guid demoUserId, CancellationToken cancellationToken = default)
    {
        await WipeAsync(context, cancellationToken);
        await SeedAsync(context, demoUserId, cancellationToken);
    }

    /// <summary>
    /// Hard delete, on purpose: the entities are soft-deleted in normal use, but a
    /// recycled sandbox must not carry the previous visitor's rows in any form.
    /// Children go first so the foreign keys stay satisfied.
    /// </summary>
    public static async Task WipeAsync(CompanyDbContext context, CancellationToken cancellationToken = default)
    {
        // Ön muhasebe tarafı önce: fatura satırları ve hareketler, sonra
        // başlıkları. Kasa/banka satırları cariye bağlı olabildiği için onlar da
        // bu temizlikten sonra siliniyor.
        await context.Database.ExecuteSqlRawAsync("DELETE FROM [StockTransactions]", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("DELETE FROM [ContactTransactions]", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("DELETE FROM [InvoiceLines]", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("DELETE FROM [Invoices]", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("DELETE FROM [Contacts]", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("DELETE FROM [Products]", cancellationToken);

        await context.Database.ExecuteSqlRawAsync("DELETE FROM [BankDetails]", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("DELETE FROM [Banks]", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("DELETE FROM [CashRegisterDetails]", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("DELETE FROM [CashRegisters]", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("DELETE FROM [Categories]", cancellationToken);
    }

    private static async Task SeedAsync(CompanyDbContext context, Guid demoUserId, CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.Now;

        // Kalemler önce yazılıyor: hareketler kimliklerine bağlanacak. Demoda da
        // olmaları önemli, yoksa ziyaretçi filtrelemenin ne işe yaradığını göremiyor.
        Dictionary<string, Guid> categories = await SeedCategoriesAsync(context, demoUserId, now, cancellationToken);

        var cashRegisters = new[]
        {
            BuildCashRegister(categories, "Merkez Kasa", CurrencyTypeEnum.TL, demoUserId, now, new Movement[]
            {
                new(88, "Açılış bakiyesi", 180_000m, 0m, "Açılış"),
                new(62, "Perakende satış tahsilatı", 46_500m, 0m, "Satış"),
                new(48, "Ofis kira ödemesi", 0m, 32_000m, "Kira"),
                new(31, "ABC Ltd. Şti. tahsilatı", 78_250m, 0m, "Tahsilat"),
                new(19, "Personel avans ödemesi", 0m, 15_000m, "Personel"),
                new(7, "Kırtasiye ve temizlik gideri", 0m, 4_380m, "Diğer Gider"),
                new(2, "Deniz Tekstil peşin satış", 23_900m, 0m, "Satış"),
            }),
            BuildCashRegister(categories, "Döviz Kasası", CurrencyTypeEnum.USD, demoUserId, now, new Movement[]
            {
                new(74, "Açılış bakiyesi", 12_000m, 0m, "Açılış"),
                new(40, "İhracat bedeli tahsilatı", 8_400m, 0m, "İhracat"),
                new(12, "Yurt dışı fuar gideri", 0m, 3_150m, "Diğer Gider"),
            }),
            BuildCashRegister(categories, "Euro Kasası", CurrencyTypeEnum.EUR, demoUserId, now, new Movement[]
            {
                new(66, "Açılış bakiyesi", 7_500m, 0m, "Açılış"),
                new(23, "Almanya bayi tahsilatı", 4_250m, 0m, "Tahsilat"),
            }),
        };

        var banks = new[]
        {
            BuildBank(categories, "Ziraat Bankası - Vadesiz TL", "TR330006100519786457841326", CurrencyTypeEnum.TL, demoUserId, now, new Movement[]
            {
                new(90, "Açılış bakiyesi", 640_000m, 0m, "Açılış"),
                new(57, "XYZ A.Ş. tedarikçi ödemesi", 0m, 196_400m, "Tedarikçi"),
                new(44, "Müşteri havalesi - Marmara Gıda", 132_750m, 0m, "Tahsilat"),
                new(26, "SGK prim ödemesi", 0m, 58_900m, "Vergi ve SGK"),
                new(11, "Kredi taksiti", 0m, 47_500m, "Diğer Gider"),
                new(3, "Müşteri havalesi - Ege Lojistik", 91_200m, 0m, "Tahsilat"),
            }),
            BuildBank(categories, "Garanti BBVA - Vadesiz TL", "TR120006200119000006672315", CurrencyTypeEnum.TL, demoUserId, now, new Movement[]
            {
                new(83, "Açılış bakiyesi", 275_000m, 0m, "Açılış"),
                new(35, "Elektrik ve doğalgaz ödemesi", 0m, 21_640m, "Fatura"),
                new(16, "POS gün sonu aktarımı", 64_300m, 0m, "Satış"),
            }),
            BuildBank(categories, "İş Bankası - USD Hesabı", "TR640006400000112345678901", CurrencyTypeEnum.USD, demoUserId, now, new Movement[]
            {
                new(70, "Açılış bakiyesi", 45_000m, 0m, "Açılış"),
                new(29, "Yurt dışı havale - Nordic Supplies", 0m, 12_800m, "Tedarikçi"),
                new(9, "İhracat tahsilatı", 18_650m, 0m, "İhracat"),
            }),
        };

        await context.CashRegisters.AddRangeAsync(cashRegisters, cancellationToken);
        await context.Banks.AddRangeAsync(banks, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // Cari, ürün ve faturalar: kasa/banka tek başına ön muhasebe değil, ve
        // ziyaretçi asıl ekranlara boş girmemeli.
        await DemoAccountingSeeder.SeedAsync(
            context, demoUserId, cashRegisters, banks, cancellationToken);
    }

    /// <summary>Demo kalemleri; adları hareketlerdekiyle birebir eşleşmeli.</summary>
    private static async Task<Dictionary<string, Guid>> SeedCategoriesAsync(
        CompanyDbContext context, Guid demoUserId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        (string Name, int Direction)[] items =
        [
            ("Satış", 0),
            ("Tahsilat", 0),
            ("İhracat", 0),
            ("Açılış", 0),
            ("Kira", 1),
            ("Personel", 1),
            ("Tedarikçi", 1),
            ("Vergi ve SGK", 1),
            ("Fatura", 1),
            ("Diğer Gider", 1)
        ];

        List<Category> categories = items.Select(item => new Category
        {
            Name = item.Name,
            Direction = item.Direction,
            CreatedAt = now,
            CreatedBy = demoUserId
        }).ToList();

        await context.Categories.AddRangeAsync(categories, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return categories.ToDictionary(c => c.Name, c => c.Id);
    }

    private static CashRegister BuildCashRegister(
        Dictionary<string, Guid> categories,
        string name,
        CurrencyTypeEnum currencyType,
        Guid demoUserId,
        DateTimeOffset now,
        Movement[] movements)
    {
        var cashRegister = new CashRegister
        {
            Name = name,
            CurrencyType = currencyType,
            DepositAmount = movements.Sum(m => m.Deposit),
            WithdrawalAmount = movements.Sum(m => m.Withdrawal),
            CreatedAt = now.AddDays(-movements.Max(m => m.DaysAgo)),
            CreatedBy = demoUserId,
            Details = movements.Select(m => new CashRegisterDetail
            {
                Date = DateOnly.FromDateTime(now.AddDays(-m.DaysAgo).DateTime),
                Description = m.Description,
                DepositAmount = m.Deposit,
                WithdrawalAmount = m.Withdrawal,
                CreatedAt = now.AddDays(-m.DaysAgo),
                CreatedBy = demoUserId,
                CategoryId = categories.TryGetValue(m.Category, out Guid id) ? id : null,
            }).ToList()
        };

        return cashRegister;
    }

    private static Bank BuildBank(
        Dictionary<string, Guid> categories,
        string name,
        string iban,
        CurrencyTypeEnum currencyType,
        Guid demoUserId,
        DateTimeOffset now,
        Movement[] movements)
    {
        var bank = new Bank
        {
            Name = name,
            IBAN = iban,
            CurrencyType = currencyType,
            DepositAmount = movements.Sum(m => m.Deposit),
            WithdrawalAmount = movements.Sum(m => m.Withdrawal),
            CreatedAt = now.AddDays(-movements.Max(m => m.DaysAgo)),
            CreatedBy = demoUserId,
            Details = movements.Select(m => new BankDetail
            {
                Date = DateOnly.FromDateTime(now.AddDays(-m.DaysAgo).DateTime),
                Description = m.Description,
                DepositAmount = m.Deposit,
                WithdrawalAmount = m.Withdrawal,
                CreatedAt = now.AddDays(-m.DaysAgo),
                CreatedBy = demoUserId,
                CategoryId = categories.TryGetValue(m.Category, out Guid id) ? id : null,
            }).ToList()
        };

        return bank;
    }
}
