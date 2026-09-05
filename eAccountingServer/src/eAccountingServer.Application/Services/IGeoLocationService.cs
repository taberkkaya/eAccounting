namespace eAccountingServer.Application.Services;

/// <summary>Bir IP adresinin kabaca nereye düştüğü. Alanlar bilinmiyorsa boş kalır.</summary>
public sealed record GeoLocation(string? Country, string? CountryCode, string? City);

/// <summary>
/// IP adresini ülke ve şehre çevirir. Sonuç bulunamazsa null döner; konum bilgisi
/// hiçbir akışı durdurmamalı.
/// </summary>
public interface IGeoLocationService
{
    Task<GeoLocation?> LookupAsync(string? ipAddress, CancellationToken cancellationToken = default);
}
