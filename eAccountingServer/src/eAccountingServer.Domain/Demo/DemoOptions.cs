namespace eAccountingServer.Domain.Demo;

/// <summary>Bound from the "Demo" configuration section.</summary>
public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    /// <summary>When false the demo endpoints are not mapped and no slots are provisioned.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Number of pre-migrated tenant databases kept ready. It is also the hard ceiling
    /// on concurrent visitors: one visitor holds exactly one slot.
    /// </summary>
    public int SlotCount { get; set; } = 5;

    public string DatabaseNamePrefix { get; set; } = "eAccounting_Demo_Slot";

    /// <summary>SQL Server instance the slot databases are created on. Falls back to the main connection string's server.</summary>
    public string? DatabaseServer { get; set; }

    /// <summary>
    /// SQL login for the sandbox databases. Left unset the demo reuses whatever the main
    /// connection string uses, which is what a single-server deployment wants.
    /// </summary>
    public string? DatabaseUsername { get; set; }

    public string? DatabasePassword { get; set; }

    /// <summary>Write operations (create/update/delete) a single session may perform before it is stopped.</summary>
    public int WriteLimit { get; set; } = 40;

    /// <summary>Writes after which the client is nudged towards the contact page, without ending the session.</summary>
    public int NudgeAfterWrites { get; set; } = 15;

    public int IdleTimeoutMinutes { get; set; } = 15;

    public int AbsoluteTimeoutMinutes { get; set; } = 45;

    /// <summary>
    /// Working set ceiling. Above it the janitor evicts the least recently used sessions,
    /// which is what keeps an unattended public demo from growing without bound.
    /// </summary>
    public int MemoryThresholdMegabytes { get; set; } = 1024;

    public int JanitorIntervalSeconds { get; set; } = 30;

    public const string DefaultContactUrl = "https://ataberkkaya.com";

    /// <summary>Where an ended session sends the visitor for more detail about the project.</summary>
    public string ContactUrl { get; set; } = DefaultContactUrl;

    public string UserName { get; set; } = "demo";

    public string Email { get; set; } = "demo@ataberkkaya.com";
}
