using eAccountingServer.Domain.Demo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAccountingServer.Infrastructure.Configuration;

internal sealed class DemoVisitorConfiguration : IEntityTypeConfiguration<DemoVisitor>
{
    public void Configure(EntityTypeBuilder<DemoVisitor> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        // Adres başına tek satır; tekrar gelen ziyaretçi yeni kayıt açmaz.
        builder.Property(p => p.Email).HasMaxLength(254).IsRequired();
        builder.HasIndex(p => p.Email).IsUnique();

        builder.Property(p => p.DisplayEmail).HasMaxLength(254);
        builder.Property(p => p.CodeHash).HasMaxLength(64);
        builder.Property(p => p.IpAddress).HasMaxLength(45);
        builder.Property(p => p.UserAgent).HasMaxLength(400);

        // Hesaplanan özellik; sütunu yok.
        builder.Ignore(p => p.IsVerified);
    }
}
