using eAccountingServer.Domain.Demo;

namespace eAccountingServer.Application.Services;

public sealed record DemoSessionStartResult(string AccessToken, DemoSessionStatus Status);

public interface IDemoSessionService
{
    bool Enabled { get; }

    /// <summary>Leases a sandbox database and mints a token scoped to it.</summary>
    Task<DemoSessionStartResult> StartAsync(CancellationToken cancellationToken = default);

    DemoSessionStatus? GetStatus(Guid sessionId);

    bool IsAlive(Guid sessionId);

    /// <summary>Records activity so the idle timeout is measured from the last real request.</summary>
    void Touch(Guid sessionId);

    /// <summary>
    /// Counts one write against the session's quota. Returns false when the quota is
    /// already spent, in which case the caller must reject the request.
    /// </summary>
    bool TryRegisterWrite(Guid sessionId);

    /// <summary>Releases the session's sandbox and resets it for the next visitor.</summary>
    Task<bool> EndAsync(Guid sessionId, DemoSessionEndReason reason, CancellationToken cancellationToken = default);
}
