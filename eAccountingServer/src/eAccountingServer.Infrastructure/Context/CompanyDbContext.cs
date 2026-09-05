using System.Security.Claims;
using eAccountingServer.Domain.Abstractions;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Enums;
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
        CreateConnectionString(httpContextAccessor, context);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Bağlantı kurulamadıysa sebebini burada söylemek gerekiyor: aksi hâlde
        // sorgu anında sürücünün anlamsız hatası kullanıcıya kadar gidiyor.
        if (string.IsNullOrEmpty(connectionString))
            throw new CompanyNotSelectedException();

        optionsBuilder.UseSqlServer(connectionString);
    }

    public DbSet<CashRegister> CashRegisters { get; set; }
    public DbSet<CashRegisterDetail> CashRegisterDetails { get; set; }
    public DbSet<Bank> Banks { get; set; }
    public DbSet<BankDetail> BankDetails { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        #region CashRegister
        modelBuilder.Entity<CashRegister>().Property(p => p.DepositAmount).HasColumnType("money");
        modelBuilder.Entity<CashRegister>().Property(p => p.WithdrawalAmount).HasColumnType("money");
        modelBuilder.Entity<CashRegister>()
            .Property(p => p.CurrencyType)
            .HasConversion(type => type.Value, value => CurrencyTypeEnum.FromValue(value));
        modelBuilder.Entity<CashRegister>().HasMany(p => p.Details).WithOne().HasForeignKey(p => p.CashRegisterId);
        modelBuilder.Entity<CashRegister>().HasQueryFilter(p => !p.IsDeleted);
        #endregion

        #region CashRegisterDetail
        modelBuilder.Entity<CashRegisterDetail>().Property(p => p.DepositAmount).HasColumnType("money");
        modelBuilder.Entity<CashRegisterDetail>().Property(p => p.WithdrawalAmount).HasColumnType("money");
        modelBuilder.Entity<CashRegisterDetail>().HasQueryFilter(p => !p.IsDeleted);
        #endregion

        #region Bank    
        modelBuilder.Entity<Bank>().Property(p => p.DepositAmount).HasColumnType("money");
        modelBuilder.Entity<Bank>().Property(p => p.WithdrawalAmount).HasColumnType("money");
        modelBuilder.Entity<Bank>()
            .Property(p => p.CurrencyType)
            .HasConversion(type => type.Value, value => CurrencyTypeEnum.FromValue(value));
        modelBuilder.Entity<Bank>().HasMany(p => p.Details).WithOne().HasForeignKey(p => p.BankId);
        modelBuilder.Entity<Bank>().HasQueryFilter(p => !p.IsDeleted);
        #endregion

        #region BankDetail
        modelBuilder.Entity<BankDetail>().Property(p => p.DepositAmount).HasColumnType("money");
        modelBuilder.Entity<BankDetail>().Property(p => p.WithdrawalAmount).HasColumnType("money");
        modelBuilder.Entity<BankDetail>().HasQueryFilter(p => !p.IsDeleted);
        #endregion

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
                $"Password={company.Database.Password};" +
                $"Connect Timeout=30;" +
                $"Encrypt=True;" +
                $"Trust Server Certificate=True;" +
                $"Application Intent=ReadWrite;" +
                $"Multi Subnet Failover=False";
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<Entity>();

        HttpContextAccessor httpContextAccessor = new();
        Guid userId = Guid.TryParse(
           httpContextAccessor.HttpContext?.User?.Claims?.FirstOrDefault(p => p.Type == "Id")?.Value,
           out var parsedUserId) ? parsedUserId : Guid.Empty;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                // Only stamp what the caller left unset, so seeded rows can carry
                // their own historical timestamps and authorship.
                if (entry.Property(p => p.CreatedAt).CurrentValue == default)
                    entry.Property(p => p.CreatedAt)
                        .CurrentValue = DateTimeOffset.Now;

                if (entry.Property(p => p.CreatedBy).CurrentValue == Guid.Empty)
                    entry.Property(p => p.CreatedBy)
                        .CurrentValue = userId;
            }

            if (entry.State == EntityState.Modified)
            {

                if (entry.Property(p => p.IsDeleted).CurrentValue == true)
                {
                    entry.Property(p => p.DeletedAt)
                        .CurrentValue = DateTimeOffset.Now;
                    entry.Property(p => p.DeletedBy)
                        .CurrentValue = userId;
                }

                else
                {
                    entry.Property(p => p.UpdatedAt)
                        .CurrentValue = DateTimeOffset.Now;
                    entry.Property(p => p.UpdatedBy)
                        .CurrentValue = userId;
                }
            }

            //if (entry.State == EntityState.Deleted)
            //    throw new ArgumentException("Database üzerinden hard delete yapamazsınız.");
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

