using System.Security.Cryptography;
using System.Threading.RateLimiting;
using eAccountingServer.Application;
using eAccountingServer.Infrastructure;
using eAccountingServer.WebApi;
using eAccountingServer.WebApi.Middlewares;
using eAccountingServer.WebApi.Modules;
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
});

builder.Services.AddExceptionHandler<ExceptionHandler>().AddProblemDetails();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapDefaultEndpoints();

// A container that only listens on HTTP would redirect every request into a dead end.
if (app.Configuration.GetValue("UseHttpsRedirection", true))
    app.UseHttpsRedirection();

app.UseCors(policy =>
{
    string[] allowedOrigins = app.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    policy.AllowAnyHeader().AllowAnyMethod();

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
