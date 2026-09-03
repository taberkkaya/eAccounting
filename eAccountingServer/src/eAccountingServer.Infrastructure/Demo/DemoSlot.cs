namespace eAccountingServer.Infrastructure.Demo;

/// <summary>
/// One pre-migrated tenant database that a visitor can borrow for the length of a
/// session. Slots are fixed and recycled rather than created per visitor, so the demo
/// never issues DDL on a request path and its footprint is bounded by SlotCount.
/// </summary>
internal sealed class DemoSlot
{
    public required int Index { get; init; }
    public required Guid CompanyId { get; init; }
    public required string CompanyName { get; init; }
    public required string DatabaseName { get; init; }

    public Guid? SessionId { get; set; }

    public bool IsFree => SessionId is null;
}
