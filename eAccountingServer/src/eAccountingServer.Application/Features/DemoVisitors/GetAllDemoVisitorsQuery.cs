using MediatR;
using ResultKit;

namespace eAccountingServer.Application.Features.DemoVisitors;

public sealed record GetAllDemoVisitorsQuery() : IRequest<Result<List<DemoVisitorDto>>>;

/// <summary>
/// Yöneticiye gösterilen hâli. Bekleyen kodun karması ve süresi dışarı
/// çıkmamalı; listede işi olan tek şey kimin ne zaman denediği.
/// </summary>
public sealed record DemoVisitorDto(
    Guid Id,
    string Email,
    bool IsVerified,
    DateTimeOffset? VerifiedAt,
    int CodesSent,
    int SessionCount,
    DateTimeOffset? LastSessionAt,
    DateTimeOffset FirstSeenAt,
    string? IpAddress,
    string? UserAgent,
    string? Country,
    string? CountryCode,
    string? City);

/// <summary>Ziyaretçi deposunu okumak için; uygulama katmanı bağlamı görmez.</summary>
public interface IDemoVisitorReader
{
    Task<List<DemoVisitorDto>> ListAsync(CancellationToken cancellationToken = default);
}

internal sealed class GetAllDemoVisitorsQueryHandler(
    IDemoVisitorReader reader
    ) : IRequestHandler<GetAllDemoVisitorsQuery, Result<List<DemoVisitorDto>>>
{
    public async Task<Result<List<DemoVisitorDto>>> Handle(
        GetAllDemoVisitorsQuery request, CancellationToken cancellationToken) =>
        await reader.ListAsync(cancellationToken);
}
