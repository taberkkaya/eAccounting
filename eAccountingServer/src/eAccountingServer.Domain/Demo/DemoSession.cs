namespace eAccountingServer.Domain.Demo;

public enum DemoSessionEndReason
{
    None = 0,
    WriteLimitReached = 1,
    Expired = 2,
    Idle = 3,
    MemoryPressure = 4,
    SlotReclaimed = 5,
    VisitorReset = 6
}

/// <summary>
/// A single visitor's run through the demo. Sessions are deliberately in-memory: they
/// are worthless after a restart and persisting them would add write traffic to the
/// main database for data nobody reads.
/// </summary>
public sealed class DemoSession
{
    public required Guid Id { get; init; }
    public required int SlotIndex { get; init; }
    public required Guid CompanyId { get; init; }
    public required string CompanyName { get; init; }
    public required Guid UserId { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }

    public DateTimeOffset LastSeenAt { get; set; }
    public int WriteCount { get; set; }
    public DemoSessionEndReason EndReason { get; set; } = DemoSessionEndReason.None;
}

/// <summary>What the client needs to render the demo banner and the contact prompt.</summary>
public sealed record DemoSessionStatus(
    Guid SessionId,
    string CompanyName,
    int WritesUsed,
    int WriteLimit,
    int NudgeAfterWrites,
    DateTimeOffset ExpiresAt,
    int SecondsRemaining,
    string ContactUrl,
    bool IsActive,
    string? EndReason);
