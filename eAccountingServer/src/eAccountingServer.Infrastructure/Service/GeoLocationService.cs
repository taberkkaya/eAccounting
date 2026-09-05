using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Demo;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eAccountingServer.Infrastructure.Service;

/// <summary>
/// Adresi dışarıdaki bir servise sorar. Sonuç adres başına önbelleğe alınır; aynı
/// ziyaretçi birkaç kez kod istediğinde tekrar sorulmaz.
///
/// Arama başarısız olursa null döner ve çağıran yoluna devam eder: konum bilgisi
/// hiçbir zaman demoya girmenin önünde duracak kadar önemli değil.
/// </summary>
internal sealed class GeoLocationService(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IOptions<DemoOptions> demoOptions,
    ILogger<GeoLocationService> logger
    ) : IGeoLocationService
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(12);

    private readonly DemoOptions _options = demoOptions.Value;

    public async Task<GeoLocation?> LookupAsync(
        string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.GeoLookupUrl)) return null;
        if (!IsPublic(ipAddress)) return null;

        string key = $"geo:{ipAddress}";
        if (cache.TryGetValue(key, out GeoLocation? cached)) return cached;

        GeoLocation? location = await QueryAsync(ipAddress!, cancellationToken);

        // Başarısız arama da önbelleğe giriyor: servis kapalıyken her istekte
        // yeniden beklemenin anlamı yok.
        cache.Set(key, location, CacheLifetime);

        return location;
    }

    private async Task<GeoLocation?> QueryAsync(string ipAddress, CancellationToken cancellationToken)
    {
        string url = _options.GeoLookupUrl.Replace("{ip}", Uri.EscapeDataString(ipAddress));

        try
        {
            using HttpClient client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.GeoLookupTimeoutSeconds, 1, 15));

            using HttpResponseMessage response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            GeoResponse? payload = await JsonSerializer.DeserializeAsync<GeoResponse>(
                stream, cancellationToken: cancellationToken);

            // Servis "bulamadım" cevabını da 200 ile döndürüyor.
            if (payload is null || payload.Success == false) return null;

            if (string.IsNullOrWhiteSpace(payload.Country)
                && string.IsNullOrWhiteSpace(payload.City)) return null;

            return new GeoLocation(
                Blank(payload.Country),
                Blank(payload.CountryCode),
                Blank(payload.City));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Konum aranamadı: {Ip}.", ipAddress);
            return null;
        }
    }

    /// <summary>
    /// Yerel ağ ve geri döngü adreslerini sormanın anlamı yok; hem cevap gelmez hem
    /// de dışarıya gereksiz istek çıkar.
    /// </summary>
    private static bool IsPublic(string? value)
    {
        if (!IPAddress.TryParse(value, out IPAddress? address)) return false;

        if (IPAddress.IsLoopback(address)) return false;
        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal) return false;

        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return true;

        byte[] octets = address.GetAddressBytes();

        return octets[0] switch
        {
            10 => false,
            127 => false,
            169 when octets[1] == 254 => false,
            172 when octets[1] is >= 16 and <= 31 => false,
            192 when octets[1] == 168 => false,
            // Operatör seviyesindeki NAT aralığı; buradan gelen adres de kimseyi
            // işaret etmiyor.
            100 when octets[1] is >= 64 and <= 127 => false,
            _ => true
        };
    }

    private static string? Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>ipwho.is biçimi; başka bir servise geçilirse burası da değişmeli.</summary>
    private sealed record GeoResponse
    {
        [JsonPropertyName("success")]
        public bool? Success { get; init; }

        [JsonPropertyName("country")]
        public string? Country { get; init; }

        [JsonPropertyName("country_code")]
        public string? CountryCode { get; init; }

        [JsonPropertyName("city")]
        public string? City { get; init; }
    }
}
