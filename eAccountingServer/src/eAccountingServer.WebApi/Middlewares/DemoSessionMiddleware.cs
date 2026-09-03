using System.Security.Claims;
using eAccountingServer.Application.Services;

namespace eAccountingServer.WebApi.Middlewares;

/// <summary>
/// Enforces the rules that only apply to anonymous demo visitors: the session must
/// still be alive, tenant administration stays off limits, and every write counts
/// against the session quota that eventually sends the visitor to the contact page.
/// Requests carrying a normal user token pass straight through.
/// </summary>
public sealed class DemoSessionMiddleware(RequestDelegate next)
{
    private const string SessionClaim = "DemoSessionId";

    private static readonly HashSet<string> WriteActions =
        new(StringComparer.OrdinalIgnoreCase) { "create", "update", "deletebyid", "migrateall" };

    private static readonly HashSet<string> BlockedControllers =
        new(StringComparer.OrdinalIgnoreCase) { "companies", "users" };

    public async Task InvokeAsync(HttpContext context, IDemoSessionService demoSessionService)
    {
        string? claim = context.User.FindFirstValue(SessionClaim);

        if (claim is null || !Guid.TryParse(claim, out Guid sessionId))
        {
            await next(context);
            return;
        }

        // The demo endpoints manage session lifetime themselves; gating them on a live
        // session would make it impossible to start over once one has ended.
        if (context.Request.Path.StartsWithSegments("/api/demo"))
        {
            await next(context);
            return;
        }

        if (!demoSessionService.IsAlive(sessionId))
        {
            await WriteAsync(context, StatusCodes.Status409Conflict, "session_ended",
                "Demo oturumunuz sona erdi. Yeni bir oturum başlatabilirsiniz.");
            return;
        }

        (string? controller, string? action) = ReadRoute(context.Request.Path);

        bool isWrite = action is not null && WriteActions.Contains(action);

        if (isWrite && controller is not null && BlockedControllers.Contains(controller))
        {
            await WriteAsync(context, StatusCodes.Status403Forbidden, "action_blocked",
                "Demo oturumunda firma ve kullanıcı yönetimi salt okunurdur.");
            return;
        }

        if (isWrite && !demoSessionService.TryRegisterWrite(sessionId))
        {
            await WriteAsync(context, StatusCodes.Status409Conflict, "write_limit",
                "Bu demo oturumu için ayrılan işlem hakkı doldu.");
            return;
        }

        if (!isWrite) demoSessionService.Touch(sessionId);

        await next(context);
    }

    private static (string? Controller, string? Action) ReadRoute(PathString path)
    {
        // Every business endpoint is /api/{controller}/{action}.
        string[] segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];

        return segments.Length < 3 ? (null, null) : (segments[1], segments[^1]);
    }

    private static Task WriteAsync(HttpContext context, int statusCode, string demoCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsJsonAsync(new
        {
            data = (object?)null,
            errorMessages = new[] { message },
            isSuccessful = false,
            statusCode,
            demoCode
        });
    }
}
