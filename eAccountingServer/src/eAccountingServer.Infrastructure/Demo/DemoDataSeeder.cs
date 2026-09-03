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
    private sealed record Movement(int DaysAgo, string Description, decimal Deposit, decimal Withdrawal);

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
        await context.Database.ExecuteSqlRawAsync("DELETE FROM [BankDetails]", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("DELETE FROM [Banks]", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("DELETE FROM [CashRegisterDetails]", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("DELETE FROM [CashRegisters]", cancellationToken);
    }

    private static async Task SeedAsync(CompanyDbContext context, Guid demoUserId, CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.Now;

        var cashRegisters = new[]
        {
            BuildCashRegister("Merkez Kasa", CurrencyTypeEnum.TL, demoUserId, now, new Movement[]
            {
                new(88, "Açılış bakiyesi", 180_000m, 0m),
                new(62, "Perakende satış tahsilatı", 46_500m, 0m),
                new(48, "Ofis kira ödemesi", 0m, 32_000m),
                new(31, "ABC Ltd. Şti. tahsilatı", 78_250m, 0m),
                new(19, "Personel avans ödemesi", 0m, 15_000m),
                new(7, "Kırtasiye ve temizlik gideri", 0m, 4_380m),
                new(2, "Deniz Tekstil peşin satış", 23_900m, 0m),
            }),
            BuildCashRegister("Döviz Kasası", CurrencyTypeEnum.USD, demoUserId, now, new Movement[]
            {
                new(74, "Açılış bakiyesi", 12_000m, 0m),
                new(40, "İhracat bedeli tahsilatı", 8_400m, 0m),
                new(12, "Yurt dışı fuar gideri", 0m, 3_150m),
            }),
            BuildCashRegister("Euro Kasası", CurrencyTypeEnum.EUR, demoUserId, now, new Movement[]
            {
                new(66, "Açılış bakiyesi", 7_500m, 0m),
                new(23, "Almanya bayi tahsilatı", 4_250m, 0m),
            }),
        };

        var banks = new[]
        {
            BuildBank("Ziraat Bankası - Vadesiz TL", "TR330006100519786457841326", CurrencyTypeEnum.TL, demoUserId, now, new Movement[]
            {
                new(90, "Açılış bakiyesi", 640_000m, 0m),
                new(57, "XYZ A.Ş. tedarikçi ödemesi", 0m, 196_400m),
                new(44, "Müşteri havalesi - Marmara Gıda", 132_750m, 0m),
                new(26, "SGK prim ödemesi", 0m, 58_900m),
                new(11, "Kredi taksiti", 0m, 47_500m),
                new(3, "Müşteri havalesi - Ege Lojistik", 91_200m, 0m),
            }),
            BuildBank("Garanti BBVA - Vadesiz TL", "TR120006200119000006672315", CurrencyTypeEnum.TL, demoUserId, now, new Movement[]
            {
                new(83, "Açılış bakiyesi", 275_000m, 0m),
                new(35, "Elektrik ve doğalgaz ödemesi", 0m, 21_640m),
                new(16, "POS gün sonu aktarımı", 64_300m, 0m),
            }),
            BuildBank("İş Bankası - USD Hesabı", "TR640006400000112345678901", CurrencyTypeEnum.USD, demoUserId, now, new Movement[]
            {
                new(70, "Açılış bakiyesi", 45_000m, 0m),
                new(29, "Yurt dışı havale - Nordic Supplies", 0m, 12_800m),
                new(9, "İhracat tahsilatı", 18_650m, 0m),
            }),
        };

        await context.CashRegisters.AddRangeAsync(cashRegisters, cancellationToken);
        await context.Banks.AddRangeAsync(banks, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static CashRegister BuildCashRegister(
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
            }).ToList()
        };

        return cashRegister;
    }

    private static Bank BuildBank(
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
            }).ToList()
        };

        return bank;
    }
}
