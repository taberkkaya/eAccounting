using eAccountingServer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAccountingServer.Infrastructure.Configuration;
internal sealed class CompanyUserConfiguration : IEntityTypeConfiguration<CompanyUser>
{
    public void Configure(EntityTypeBuilder<CompanyUser> builder)
    {
        builder.HasKey(x => new
        {x.AppUserId,x.CompanyId});

        builder.HasQueryFilter(x => !x.IsDeleted);

      
    }
}
