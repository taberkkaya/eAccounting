using eAccountingServer.Application.Features.DemoVisitors;
using eAccountingServer.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace eAccountingServer.Infrastructure.Demo;

/// <inheritdoc />
internal sealed class DemoVisitorReader(ApplicationDbContext context) : IDemoVisitorReader
{
    public Task<List<DemoVisitorDto>> ListAsync(CancellationToken cancellationToken = default) =>
        context.DemoVisitors
            .AsNoTracking()
            // En son hareket eden en üstte: yöneticinin aradığı sıra bu.
            .OrderByDescending(p => p.LastSessionAt ?? p.CreatedAt)
            .Select(p => new DemoVisitorDto(
                p.Id,
                p.DisplayEmail == string.Empty ? p.Email : p.DisplayEmail,
                p.VerifiedAt != null,
                p.VerifiedAt,
                p.CodesSent,
                p.SessionCount,
                p.LastSessionAt,
                p.CreatedAt,
                p.IpAddress,
                p.UserAgent,
                p.Country,
                p.CountryCode,
                p.City))
            .ToListAsync(cancellationToken);
}
