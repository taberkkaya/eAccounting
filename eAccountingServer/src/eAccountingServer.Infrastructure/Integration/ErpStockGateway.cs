using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Integration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eAccountingServer.Infrastructure.Integration;

/// <inheritdoc />
internal sealed class ErpStockGateway(
    IHttpClientFactory httpClientFactory,
    IOptions<ErpOptions> options,
    ILogger<ErpStockGateway> logger
    ) : IErpStockGateway
{
    private const string KeyHeader = "X-Erp-Key";

    /// <summary>Tezgah'ın <c>StockDirection</c> değerleri.</summary>
    private const int DirectionIn = 1;
    private const int DirectionOut = 2;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ErpOptions _options = options.Value;

    public bool Enabled =>
        _options.Enabled
        && !string.IsNullOrWhiteSpace(_options.BaseUrl)
        && !string.IsNullOrWhiteSpace(_options.ApiKey)
        && _options.DefaultDepotId is not null;

    public async Task<ErpResult> PushInvoiceAsync(
        Guid invoiceId,
        string invoiceNumber,
        bool isPurchase,
        IReadOnlyList<ErpStockLine> lines,
        CancellationToken cancellationToken = default)
    {
        if (!Enabled) return ErpResult.Skipped;

        // Hiçbir satır Tezgah'a eşlenmemişse gönderilecek bir şey de yok. Yine de
        // boş bir belge gönderilmiyor: Tezgah bunu "kalem yok" diye reddederdi ve
        // ekranda sebepsiz bir uyarı çıkardı.
        if (lines.Count == 0) return ErpResult.Skipped;

        var payload = new
        {
            documentId = invoiceId,
            documentNumber = invoiceNumber,
            direction = isPurchase ? DirectionIn : DirectionOut,
            orderId = (Guid?)null,
            lines = lines.Select(line => new
            {
                productId = line.ErpProductId,
                depotId = _options.DefaultDepotId!.Value,
                quantity = line.Quantity,
                price = line.UnitPrice
            })
        };

        return await SendAsync(
            client => client.PostAsJsonAsync(Url("stock-movements"), payload, Json, cancellationToken),
            "stok hareketi yazılamadı");
    }

    public async Task<ErpResult> RemoveInvoiceAsync(
        Guid invoiceId, CancellationToken cancellationToken = default)
    {
        if (!Enabled) return ErpResult.Skipped;

        return await SendAsync(
            client => client.DeleteAsync(Url($"stock-movements/{invoiceId}"), cancellationToken),
            "stok hareketi silinemedi");
    }

    public async Task<IReadOnlyList<ErpProduct>> GetProductsAsync(
        CancellationToken cancellationToken = default) =>
        await ReadAsync<ErpProduct>("products", cancellationToken);

    public async Task<IReadOnlyList<ErpDepot>> GetDepotsAsync(
        CancellationToken cancellationToken = default) =>
        await ReadAsync<ErpDepot>("depots", cancellationToken);

    // --- yardımcılar --------------------------------------------------------

    private async Task<ErpResult> SendAsync(
        Func<HttpClient, Task<HttpResponseMessage>> call, string what)
    {
        try
        {
            using HttpClient client = CreateClient();
            using HttpResponseMessage response = await call(client);

            if (response.IsSuccessStatusCode) return ErpResult.Ok();

            logger.LogWarning(
                "Tezgah isteği reddedildi ({Status}): {What}.", (int)response.StatusCode, what);

            return ErpResult.Fail($"Tezgah'ta {what} ({(int)response.StatusCode}).");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Tezgah'a ulaşılamadı: {What}.", what);
            return ErpResult.Fail($"Tezgah'a ulaşılamadı, {what}.");
        }
    }

    /// <summary>
    /// Okuma çağrıları hata yerine boş liste döner: ürün eşleme ekranı, Tezgah
    /// kapalıyken de açılabilmeli.
    /// </summary>
    private async Task<IReadOnlyList<T>> ReadAsync<T>(string path, CancellationToken cancellationToken)
    {
        if (!Enabled) return [];

        try
        {
            using HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync(Url(path), cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Tezgah {Path} okunamadı ({Status}).", path, (int)response.StatusCode);
                return [];
            }

            ErpEnvelope<T>? envelope = await response.Content
                .ReadFromJsonAsync<ErpEnvelope<T>>(Json, cancellationToken);

            return envelope?.Data ?? [];
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Tezgah {Path} okunamadı.", path);
            return [];
        }
    }

    private HttpClient CreateClient()
    {
        HttpClient client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 1, 60));
        client.DefaultRequestHeaders.Add(KeyHeader, _options.ApiKey);

        return client;
    }

    private string Url(string path) => $"{_options.BaseUrl.TrimEnd('/')}/api/integration/{path}";

    /// <summary>Tezgah yanıtları TS.Result zarfında geliyor.</summary>
    private sealed record ErpEnvelope<T>
    {
        [JsonPropertyName("data")]
        public List<T>? Data { get; init; }
    }
}
