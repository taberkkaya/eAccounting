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

        // Anonymous on purpose: this is the door into the sandbox.
        group.MapPost("start", async (IDemoSessionService demoSessionService, CancellationToken cancellationToken) =>
        {
            if (!demoSessionService.Enabled)
                return Results.NotFound(Result<string>.Failure(404, ["Demo modu kapalı."]));

            try
            {
                DemoSessionStartResult result = await demoSessionService.StartAsync(cancellationToken);
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
