using System.Net.Http.Json;
using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Demo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eAccountingServer.Infrastructure.Demo;

/// <inheritdoc />
/// <remarks>
/// Singleton: tek bir <see cref="IHttpClientFactory"/> üzerinden çalışıyor ve
/// oturum havuzu gibi süreç geneli bir yerden çağrılıyor.
/// </remarks>
internal sealed class DemoTelemetryPublisher(
    IHttpClientFactory httpClientFactory,
    IOptions<DemoTelemetryOptions> options,
    ILogger<DemoTelemetryPublisher> logger
    ) : IDemoTelemetryPublisher
{
    private const string KeyHeader = "X-Demo-Key";

    private readonly DemoTelemetryOptions _options = options.Value;

    private bool Configured =>
        _options.Enabled
        && !string.IsNullOrWhiteSpace(_options.BaseUrl)
        && !string.IsNullOrWhiteSpace(_options.ApiKey);

    public void SessionStarted(Guid sessionId, DateTimeOffset startedAt) =>
        Post("session", new
        {
            app = _options.AppKey,
            sessionId,
            startedAt = startedAt.UtcDateTime
        });

    public void VisitorIdentified(Guid sessionId, string email, string? country, string? city) =>
        Post("session", new
        {
            app = _options.AppKey,
            sessionId,
            email,
            country,
            city
        });

    public void SessionEnded(Guid sessionId, string reason, int writesUsed, int writeLimit) =>
        Post("session", new
        {
            app = _options.AppKey,
            sessionId,
            endedAt = DateTime.UtcNow,
            endReason = reason,
            writesUsed,
            writeLimit
        });

    public void Heartbeat(bool demoEnabled, int slotCount, int busySlots) =>
        Post("heartbeat", new
        {
            app = _options.AppKey,
            name = _options.AppName,
            url = _options.AppUrl,
            demoEnabled,
            slotCount,
            busySlots
        });

    /// <summary>
    /// İsteği arka planda yollar ve sonucunu beklemez. Çağıran bir oturumun
    /// ortasında; panel yavaş ya da kapalı olduğunda ziyaretçi bunu hissetmemeli.
    /// </summary>
    private void Post(string path, object payload)
    {
        if (!Configured) return;

        _ = Task.Run(async () =>
        {
            try
            {
                using HttpClient client = httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 1, 30));
                client.DefaultRequestHeaders.Add(KeyHeader, _options.ApiKey);

                string url = $"{_options.BaseUrl.TrimEnd('/')}/api/demo-telemetry/{path}";

                using HttpResponseMessage response = await client.PostAsJsonAsync(url, payload);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "Demo telemetrisi reddedildi ({Status}): {Path}.",
                        (int)response.StatusCode, path);
                }
            }
            catch (Exception exception)
            {
                // Yutuluyor: bu görevi kimse beklemiyor, fırlatılan hata süreç
                // genelinde gözlemlenmeyen bir istisna olurdu.
                logger.LogWarning(exception, "Demo telemetrisi gönderilemedi: {Path}.", path);
            }
        });
    }
}
