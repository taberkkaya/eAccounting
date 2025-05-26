using eAccountingServer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAccountingServer.Infrastructure.Configuration;
internal sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(p => p.TaxNumber).HasColumnType("varchar(11)");
          
        builder.OwnsOne(p => p.Database, builder =>
        {
            builder.Property(p => p.Server).HasColumnName("Server");
            builder.Property(p => p.DatabaseName).HasColumnName("DatabaseName");
            builder.Property(p => p.Username).HasColumnName("Username");
            builder.Property(p => p.Password).HasColumnName("Password");
        });
    }
}
