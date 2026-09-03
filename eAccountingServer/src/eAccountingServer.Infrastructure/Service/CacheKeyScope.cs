using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace eAccountingServer.Infrastructure.Service;

/// <summary>
/// Cache keys used by the feature handlers ("banks", "cashRegisters", ...) are plain
/// names, but the data behind them lives in a per-company database. Without a tenant
/// prefix one company's cached list would be served to the next company that asks for
/// it, so every key is scoped to the CompanyId claim of the current request.
/// </summary>
internal sealed class CacheKeyScope(IHttpContextAccessor httpContextAccessor)
{
    private const string GlobalTenant = "global";

    public string Qualify(string key) => $"{CurrentTenant()}:{key}";

    /// <summary>Suffix used to invalidate a key for every tenant at once.</summary>
    public static string Suffix(string key) => $":{key}";

    private string CurrentTenant()
    {
        string? companyId = httpContextAccessor.HttpContext?.User.FindFirstValue("CompanyId");

        return string.IsNullOrWhiteSpace(companyId) ? GlobalTenant : companyId;
    }
}
