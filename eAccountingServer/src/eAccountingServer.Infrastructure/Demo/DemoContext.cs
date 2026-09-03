using System.Security.Claims;
using eAccountingServer.Application.Services;
using Microsoft.AspNetCore.Http;

namespace eAccountingServer.Infrastructure.Demo;

internal sealed class DemoContext(IHttpContextAccessor httpContextAccessor) : IDemoContext
{
    public bool IsDemoRequest => SessionId is not null;

    public Guid? SessionId => Read(DemoClaimTypes.SessionId);

    public Guid? CompanyId => Read("CompanyId");

    private Guid? Read(string claimType)
    {
        string? value = httpContextAccessor.HttpContext?.User.FindFirstValue(claimType);

        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}
