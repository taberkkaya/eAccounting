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
    public DbSet<Category> Categories { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<ContactTransaction> ContactTransactions { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceLine> InvoiceLines { get; set; }
    public DbSet<StockTransaction> StockTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureAccounting(modelBuilder);

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

        #region Category
        modelBuilder.Entity<Category>().Property(p => p.Name).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<Category>().HasQueryFilter(p => !p.IsDeleted);
        #endregion

        #region BankDetail
        modelBuilder.Entity<BankDetail>().Property(p => p.DepositAmount).HasColumnType("money");
        modelBuilder.Entity<BankDetail>().Property(p => p.WithdrawalAmount).HasColumnType("money");
        modelBuilder.Entity<BankDetail>().HasQueryFilter(p => !p.IsDeleted);
        #endregion

    }

    /// <summary>
    /// Cari, ürün ve fatura tarafı. Kasa/banka ile aynı dosyada ama ayrı bir
    /// metotta: o taraf uygulamanın kasa defteri, burası ön muhasebe kısmı.
    /// </summary>
    private static void ConfigureAccounting(ModelBuilder modelBuilder)
    {
        #region Contact
        modelBuilder.Entity<Contact>(entity =>
        {
            entity.Property(p => p.Name).HasMaxLength(160).IsRequired();
            entity.Property(p => p.TaxNumber).HasMaxLength(20);
            entity.Property(p => p.TaxOffice).HasMaxLength(80);
            entity.Property(p => p.Phone).HasMaxLength(40);
            entity.Property(p => p.Email).HasMaxLength(160);
            entity.Property(p => p.Address).HasMaxLength(400);
            entity.Property(p => p.Note).HasMaxLength(1000);
            entity.Property(p => p.DebitAmount).HasColumnType("money");
            entity.Property(p => p.CreditAmount).HasColumnType("money");
            entity.Property(p => p.CurrencyType)
                .HasConversion(type => type.Value, value => CurrencyTypeEnum.FromValue(value));
            entity.HasMany(p => p.Transactions).WithOne().HasForeignKey(p => p.ContactId);
            entity.Ignore(p => p.Balance);
            entity.HasQueryFilter(p => !p.IsDeleted);
        });
        #endregion

        #region ContactTransaction
        modelBuilder.Entity<ContactTransaction>(entity =>
        {
            entity.Property(p => p.Description).HasMaxLength(400).IsRequired();
            entity.Property(p => p.DebitAmount).HasColumnType("money");
            entity.Property(p => p.CreditAmount).HasColumnType("money");
            entity.HasIndex(p => new { p.ContactId, p.Date });
            entity.HasQueryFilter(p => !p.IsDeleted);
        });
        #endregion

        #region Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Code).HasMaxLength(40);
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Unit).HasMaxLength(20).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(1000);
            entity.Property(p => p.PurchasePrice).HasColumnType("money");
            entity.Property(p => p.SalePrice).HasColumnType("money");
            entity.Property(p => p.StockQuantity).HasPrecision(18, 3);
            entity.Property(p => p.CriticalStock).HasPrecision(18, 3);
            entity.Property(p => p.CurrencyType)
                .HasConversion(type => type.Value, value => CurrencyTypeEnum.FromValue(value));
            entity.HasQueryFilter(p => !p.IsDeleted);
        });
        #endregion

        #region Invoice
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.Property(p => p.Number).HasMaxLength(30).IsRequired();
            entity.Property(p => p.Note).HasMaxLength(1000);
            entity.Property(p => p.SubTotal).HasColumnType("money");
            entity.Property(p => p.DiscountTotal).HasColumnType("money");
            entity.Property(p => p.VatTotal).HasColumnType("money");
            entity.Property(p => p.GrandTotal).HasColumnType("money");
            entity.Property(p => p.PaidAmount).HasColumnType("money");
            entity.Property(p => p.CurrencyType)
                .HasConversion(type => type.Value, value => CurrencyTypeEnum.FromValue(value));
            entity.HasOne(p => p.Contact).WithMany().HasForeignKey(p => p.ContactId);
            entity.HasMany(p => p.Lines).WithOne().HasForeignKey(p => p.InvoiceId);
            entity.HasIndex(p => new { p.Type, p.Number });
            entity.Ignore(p => p.RemainingAmount);
            entity.HasQueryFilter(p => !p.IsDeleted);
        });
        #endregion

        #region InvoiceLine
        modelBuilder.Entity<InvoiceLine>(entity =>
        {
            entity.Property(p => p.Description).HasMaxLength(400).IsRequired();
            entity.Property(p => p.Unit).HasMaxLength(20).IsRequired();
            entity.Property(p => p.Quantity).HasPrecision(18, 3);
            entity.Property(p => p.DiscountRate).HasPrecision(5, 2);
            entity.Property(p => p.UnitPrice).HasColumnType("money");
            entity.Property(p => p.LineTotal).HasColumnType("money");
            entity.Property(p => p.VatAmount).HasColumnType("money");
            entity.HasQueryFilter(p => !p.IsDeleted);
        });
        #endregion

        #region StockTransaction
        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.Property(p => p.Description).HasMaxLength(400).IsRequired();
            entity.Property(p => p.Quantity).HasPrecision(18, 3);
            entity.Property(p => p.UnitPrice).HasColumnType("money");
            entity.HasIndex(p => new { p.ProductId, p.Date });
            entity.HasQueryFilter(p => !p.IsDeleted);
        });
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

