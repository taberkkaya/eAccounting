using eAccountingServer.Domain.Abstractions;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Users;
using GenericRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace eAccountingServer.Infrastructure.Context
{
    internal sealed class ApplicationDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>, IUnitOfWork
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<CompanyUser> CompanyUsers { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            modelBuilder.Ignore<IdentityUserClaim<Guid>>();
            modelBuilder.Ignore<IdentityRoleClaim<Guid>>();
            modelBuilder.Ignore<IdentityUserRole<Guid>>();
            modelBuilder.Ignore<IdentityUserToken<Guid>>();
            modelBuilder.Ignore<IdentityUserLogin<Guid>>();
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
                    entry.Property(p => p.CreatedAt)
                        .CurrentValue = DateTimeOffset.Now;
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
}
