using System.Security.Cryptography;
using System.Threading.RateLimiting;
using eAccountingServer.Application;
using eAccountingServer.Infrastructure;
using eAccountingServer.WebApi;
using eAccountingServer.WebApi.Middlewares;
using eAccountingServer.WebApi.Modules;
using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// The signing key is never committed. A deployment must supply Jwt__SecretKey; local
// development falls back to an ephemeral key so the API still starts with no setup.
if (string.IsNullOrWhiteSpace(builder.Configuration["Jwt:SecretKey"]))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException(
            "Jwt:SecretKey is not configured. Set the Jwt__SecretKey environment variable.");

    builder.Configuration["Jwt:SecretKey"] = RandomNumberGenerator.GetHexString(128);
}

builder.Services.AddResponseCompression(
    opt =>
    {
        opt.EnableForHttps = true;
    }
    );

builder.AddServiceDefaults();
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCors();
builder.Services.AddOpenApi();
builder.Services
    .AddControllers();

// Partitioned by caller so one visitor hammering the public demo cannot spend the
// whole window on everybody else's behalf.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("fixed", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            QueueLimit = 50,
            Window = TimeSpan.FromSeconds(1),
            PermitLimit = 30,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        }));

    // Kod gönderen uç mail üretiyor: genel sınır burada fazla cömert kalırdı ve
    // adres sahibinin kutusunu doldurmak için kullanılabilirdi.
    options.AddPolicy("demo-code", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            QueueLimit = 0,
            Window = TimeSpan.FromMinutes(15),
            PermitLimit = 6,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        }));
});

// Uygulama bir ters vekilin arkasında çalışıyor; bu olmadan istemcinin IP'si
// yerine vekilin konteyner adresi görülür. Ziyaretçi kaydı da hız sınırı da
// gerçek adrese bakmak zorunda.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Zincirde kaç vekil olduğu dağıtıma göre değişiyor (yerelde yalnız Caddy,
    // Coolify'da önünde bir de Traefik). Sabit bir sayı yazmak yerine vekiller
    // adreslerinden tanınıyor: başlık sağdan sola, özel ağ adresleri atlanarak
    // okunuyor ve ilk genel adreste duruluyor.
    //
    // Bunun yan faydası, istemcinin kendi uydurduğu bir X-Forwarded-For değerine
    // hiç ulaşılmaması: o değer zincirde her zaman gerçek adresin solunda kalır.
    options.ForwardLimit = null;
    options.KnownProxies.Clear();
    options.KnownNetworks.Clear();

    foreach ((string prefix, int length) in TrustedProxyNetworks())
        options.KnownNetworks.Add(
            new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse(prefix), length));
});

builder.Services.AddExceptionHandler<ExceptionHandler>().AddProblemDetails();

var app = builder.Build();

// Adresi okuyan her şeyden önce gelmeli.
if (app.Configuration.GetValue("Network:TrustForwardedHeaders", true))
    app.UseForwardedHeaders();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapDefaultEndpoints();

// Konteynerin hazır olduğunu söyleyen uç. /api altında değil, yani öndeki vekil
// bunu dışarı taşımıyor: yalnızca Docker ağından, sağlık kontrolünden görülür.
// Dağıtım sırasında yeni konteynerin ne zaman trafik alabileceği buradan anlaşılıyor.
app.MapGet("/health", () => Results.Text("Healthy")).AllowAnonymous();

// A container that only listens on HTTP would redirect every request into a dead end.
if (app.Configuration.GetValue("UseHttpsRedirection", true))
    app.UseHttpsRedirection();

app.UseCors(policy =>
{
    string[] allowedOrigins = app.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    policy.AllowAnyHeader().AllowAnyMethod();

    // Rapor indirmelerinde dosya adı bu başlıktan okunuyor; farklı kaynaktan
    // çağrıldığında tarayıcı başlığı açıkça izin verilmedikçe gizliyor.
    policy.WithExposedHeaders("Content-Disposition");

    if (allowedOrigins.Length > 0)
        policy.WithOrigins(allowedOrigins).AllowCredentials();
    else
        // The API is authenticated with bearer tokens rather than cookies, so a wide
        // open origin list is only safe while credentials stay off.
        policy.AllowAnyOrigin();
});

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<DemoSessionMiddleware>();

app.RegisterRoutes();

app.UseResponseCompression();

app.UseExceptionHandler();

app.MapControllers().RequireRateLimiting("fixed").RequireAuthorization();

ExtensionsMiddleware.MigrateDatabase(app);
ExtensionsMiddleware.CreateFirstUser(app);

app.Run();

/// <summary>
/// Vekil olabilecek adres aralıkları: yönlendirilebilir olmayan her şey. Bir
/// konteyner ağında vekilin adresi her başlatmada değiştiği için tek tek
/// yazılamaz, ama bu aralıkların dışından gelen bir adres de vekil değildir.
/// </summary>
static (string Prefix, int Length)[] TrustedProxyNetworks() =>
[
    ("10.0.0.0", 8),
    ("172.16.0.0", 12),
    ("192.168.0.0", 16),
    ("127.0.0.0", 8),
    ("100.64.0.0", 10),
    ("::1", 128),
    ("fc00::", 7),
    ("fe80::", 10)
];
