using System.Security.Claims;
using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Demo;
using ResultKit;

namespace eAccountingServer.WebApi.Modules;

public static class DemoModule
{
    private const string SessionClaim = "DemoSessionId";

    public static void RegisterDemoRoutes(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("api/demo").WithTags("demo");

        // Anonim: ziyaretçinin adresine kod göndermek demoya girmenin ilk adımı.
        group.MapPost("request-code", async (
            DemoCodeRequest request,
            IDemoVerificationService verification,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!verification.Required)
                return Results.Ok((Result<string>)"Bu kurulumda doğrulama gerekmiyor.");

            DemoVerificationResult result = await verification.SendCodeAsync(
                request.Email,
                httpContext.Connection.RemoteIpAddress?.ToString(),
                httpContext.Request.Headers.UserAgent.ToString(),
                cancellationToken);

            return result.Succeeded
                ? Results.Ok((Result<string>)result.Message)
                : Results.BadRequest(Result<string>.Failure(400, [result.Message]));
        }).RequireRateLimiting("demo-code").Produces<Result<string>>();

        // Anonymous on purpose: this is the door into the sandbox.
        group.MapPost("start", async (
            DemoStartRequest request,
            IDemoSessionService demoSessionService,
            IDemoVerificationService verification,
            CancellationToken cancellationToken) =>
        {
            if (!demoSessionService.Enabled)
                return Results.NotFound(Result<string>.Failure(404, ["Demo modu kapalı."]));

            // Doğrulama kapalıysa (mail yapılandırılmamışsa) kapı eskisi gibi açık.
            if (verification.Required)
            {
                DemoVerificationResult check = await verification.VerifyAsync(
                    request.Email ?? string.Empty, request.Code ?? string.Empty, cancellationToken);

                if (!check.Succeeded)
                    return Results.BadRequest(Result<string>.Failure(400, [check.Message]));
            }

            try
            {
                DemoSessionStartResult result = await demoSessionService.StartAsync(cancellationToken);

                // Sayaç yalnızca gerçekten açılan oturum için artıyor.
                if (verification.Required)
                    await verification.RecordSessionAsync(request.Email!, cancellationToken);

                return Results.Ok((Result<DemoSessionStartResult>)result);
            }
            catch (InvalidOperationException exception)
            {
                // Every sandbox is busy, or provisioning never completed.
                return Results.Json(
                    Result<string>.Failure(503, [exception.Message]),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }).Produces<Result<DemoSessionStartResult>>();

        // Doğrulamanın açık olup olmadığını istemciye söyler: giriş ekranı
        // ziyaretçiden e-posta isteyip istemeyeceğine buna bakarak karar verir.
        group.MapGet("config", (IDemoSessionService demoSessionService, IDemoVerificationService verification) =>
            Results.Ok((Result<DemoConfig>)new DemoConfig(
                demoSessionService.Enabled,
                verification.Required)))
            .Produces<Result<DemoConfig>>();

        group.MapGet("status", (HttpContext httpContext, IDemoSessionService demoSessionService) =>
        {
            Guid? sessionId = ReadSessionId(httpContext);

            if (sessionId is null)
                return Results.BadRequest(Result<string>.Failure("Bu bir demo oturumu değil."));

            DemoSessionStatus? status = demoSessionService.GetStatus(sessionId.Value);

            return status is null
                ? Results.Json(
                    Result<string>.Failure(409, ["Demo oturumunuz sona erdi."]),
                    statusCode: StatusCodes.Status409Conflict)
                : Results.Ok((Result<DemoSessionStatus>)status);
        }).RequireAuthorization().Produces<Result<DemoSessionStatus>>();

        // Lets the visitor wipe the sandbox and start over without waiting for a timeout.
        group.MapPost("reset", async (
            HttpContext httpContext,
            IDemoSessionService demoSessionService,
            CancellationToken cancellationToken) =>
        {
            Guid? sessionId = ReadSessionId(httpContext);

            if (sessionId is not null)
                await demoSessionService.EndAsync(sessionId.Value, DemoSessionEndReason.VisitorReset, cancellationToken);

            try
            {
                DemoSessionStartResult result = await demoSessionService.StartAsync(cancellationToken);
                return Results.Ok((Result<DemoSessionStartResult>)result);
            }
            catch (InvalidOperationException exception)
            {
                return Results.Json(
                    Result<string>.Failure(503, [exception.Message]),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }).RequireAuthorization().Produces<Result<DemoSessionStartResult>>();

        group.MapPost("end", async (
            HttpContext httpContext,
            IDemoSessionService demoSessionService,
            CancellationToken cancellationToken) =>
        {
            Guid? sessionId = ReadSessionId(httpContext);

            if (sessionId is not null)
                await demoSessionService.EndAsync(sessionId.Value, DemoSessionEndReason.VisitorReset, cancellationToken);

            return Results.Ok((Result<string>)"Demo oturumu kapatıldı.");
        }).RequireAuthorization().Produces<Result<string>>();
    }

    private static Guid? ReadSessionId(HttpContext httpContext) =>
        Guid.TryParse(httpContext.User.FindFirstValue(SessionClaim), out Guid sessionId) ? sessionId : null;
}

public sealed record DemoCodeRequest(string Email);

/// <summary>Doğrulama kapalıyken iki alan da boş gelebilir.</summary>
public sealed record DemoStartRequest(string? Email, string? Code);

public sealed record DemoConfig(bool Enabled, bool EmailVerificationRequired);
