namespace eAccountingServer.Application.Services;

/// <summary>
/// Tells a handler whether it is serving an anonymous demo visitor rather than a real
/// user, so admin-wide lists can be narrowed to the visitor's own sandbox.
/// </summary>
public interface IDemoContext
{
    bool IsDemoRequest { get; }
    Guid? SessionId { get; }
    Guid? CompanyId { get; }
}
