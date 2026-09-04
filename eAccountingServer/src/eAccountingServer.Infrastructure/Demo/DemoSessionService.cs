using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Demo;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Users;
using eAccountingServer.Domain.ValueObjects;
using eAccountingServer.Infrastructure.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eAccountingServer.Infrastructure.Demo;

/// <summary>
/// Hands each anonymous visitor an isolated tenant for the length of one session and
/// takes it back when the session's quota, clock or the process' memory budget runs out.
/// Registered as a singleton: the slot pool is process-wide state.
/// </summary>
internal sealed class DemoSessionService(
    IServiceScopeFactory scopeFactory,
    IOptions<DemoOptions> demoOptions,
    ILogger<DemoSessionService> logger
    ) : IDemoSessionService
{
    private readonly DemoOptions _options = demoOptions.Value;
    private readonly ConcurrentDictionary<Guid, DemoSession> _sessions = new();
    private readonly SemaphoreSlim _slotLock = new(1, 1);

    private const string DemoCompanyName = "Demo Ticaret A.Ş.";

    private DemoSlot[] _slots = [];
    private Guid _demoUserId;
    private volatile bool _ready;

    public bool Enabled => _options.Enabled;

    public bool IsReady => _ready;

    #region provisioning

    /// <summary>
    /// Creates (or reuses) the demo user and the fixed set of sandbox databases. Called
    /// once at startup; a slot that fails to provision is skipped so one bad database
    /// cannot take the whole demo down.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        _demoUserId = await EnsureDemoUserAsync(userManager);

        DemoDatabaseTarget target = ResolveDatabaseTarget(context);
        List<DemoSlot> slots = new();

        for (int index = 1; index <= _options.SlotCount; index++)
        {
            string databaseName = $"{_options.DatabaseNamePrefix}{index:D2}";

            try
            {
                Company company = await EnsureCompanyAsync(context, target, databaseName, cancellationToken);

                using var companyContext = new CompanyDbContext(company);
                await companyContext.Database.MigrateAsync(cancellationToken);
                await DemoDataSeeder.ResetAsync(companyContext, _demoUserId, cancellationToken);

                slots.Add(new DemoSlot
                {
                    Index = index,
                    CompanyId = company.Id,
                    CompanyName = company.Name,
                    DatabaseName = databaseName
                });

                logger.LogInformation("Demo slot {Index} ready on database {Database}.", index, databaseName);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Demo slot {Index} ({Database}) could not be provisioned.", index, databaseName);
            }
        }

        _slots = [.. slots];
        _ready = _slots.Length > 0;

        if (!_ready)
            logger.LogError("No demo slot could be provisioned; the demo endpoints will report as unavailable.");
    }

    private async Task<Guid> EnsureDemoUserAsync(UserManager<AppUser> userManager)
    {
        AppUser? user = await userManager.FindByNameAsync(_options.UserName);
        if (user is not null) return user.Id;

        user = new AppUser
        {
            UserName = _options.UserName,
            Email = _options.Email,
            FirstName = "Demo",
            LastName = "Ziyaretçi",
            EmailConfirmed = true,
            IsAdmin = false,
            CreatedAt = DateTimeOffset.Now
        };
        user.CreatedBy = user.Id;

        // Never meant to be signed in with: demo tokens are minted directly, so the
        // account carries a password nobody knows.
        IdentityResult result = await userManager.CreateAsync(user, RandomNumberGenerator.GetHexString(48));

        if (!result.Succeeded)
            throw new InvalidOperationException(
                "Demo user could not be created: " + string.Join(", ", result.Errors.Select(e => e.Description)));

        return user.Id;
    }

    private async Task<Company> EnsureCompanyAsync(
        ApplicationDbContext context,
        DemoDatabaseTarget target,
        string databaseName,
        CancellationToken cancellationToken)
    {
        var database = new Database(target.Server, databaseName, target.Username, target.Password);

        Company? company = await context.Companies
            .FirstOrDefaultAsync(p => p.Database.DatabaseName == databaseName, cancellationToken);

        bool isNew = company is null;
        company ??= new Company
        {
            TaxNumber = $"90000000{databaseName[^2..]}",
            CreatedAt = DateTimeOffset.Now,
            CreatedBy = _demoUserId
        };

        // Rewritten on every startup so a slot row left behind by an earlier deployment
        // picks up the current server, credentials and presentation. Every sandbox shows
        // the same company, so the visitor sees the same name whichever slot they get.
        company.Name = DemoCompanyName;
        company.Address = "Ataşehir, İstanbul";
        company.TaxDepartment = "Kadıköy";
        company.Database = database;

        if (isNew) context.Companies.Add(company);

        await context.SaveChangesAsync(cancellationToken);

        return company;
    }

    private sealed record DemoDatabaseTarget(string Server, string Username, string Password);

    /// <summary>
    /// Sandbox databases sit next to the main one by default, reached with the same
    /// credentials, so a deployment only has to configure a connection string once.
    /// </summary>
    private DemoDatabaseTarget ResolveDatabaseTarget(ApplicationDbContext context)
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(
            context.Database.GetConnectionString());

        string server = string.IsNullOrWhiteSpace(_options.DatabaseServer)
            ? builder.DataSource
            : _options.DatabaseServer;

        string username = _options.DatabaseUsername
            ?? (builder.IntegratedSecurity ? string.Empty : builder.UserID);

        string password = _options.DatabasePassword
            ?? (builder.IntegratedSecurity ? string.Empty : builder.Password);

        return new DemoDatabaseTarget(server, username, password);
    }

    #endregion

    #region session lifetime

    public async Task<DemoSessionStartResult> StartAsync(CancellationToken cancellationToken = default)
    {
        if (!Enabled || !_ready)
            throw new InvalidOperationException("Demo mode is not available right now.");

        DemoSlot slot = await LeaseSlotAsync(cancellationToken);

        DateTimeOffset now = DateTimeOffset.Now;
        var session = new DemoSession
        {
            Id = Guid.CreateVersion7(),
            SlotIndex = slot.Index,
            CompanyId = slot.CompanyId,
            CompanyName = slot.CompanyName,
            UserId = _demoUserId,
            StartedAt = now,
            LastSeenAt = now,
            ExpiresAt = now.AddMinutes(_options.AbsoluteTimeoutMinutes)
        };

        slot.SessionId = session.Id;
        _sessions[session.Id] = session;

        string accessToken = await CreateTokenAsync(session, cancellationToken);

        logger.LogInformation(
            "Demo session {SessionId} started on slot {Slot}. Active sessions: {Active}/{Total}.",
            session.Id, slot.Index, _sessions.Count, _slots.Length);

        return new DemoSessionStartResult(accessToken, BuildStatus(session));
    }

    private async Task<DemoSlot> LeaseSlotAsync(CancellationToken cancellationToken)
    {
        await _slotLock.WaitAsync(cancellationToken);

        DemoSlot slot;
        try
        {
            slot = _slots.FirstOrDefault(s => s.IsFree)
                   ?? await ReclaimOldestSlotAsync(cancellationToken);

            // Claimed inside the lock so two visitors cannot lease the same slot; the
            // real session id replaces this marker once the session object exists.
            slot.SessionId = Guid.Empty;
        }
        finally
        {
            _slotLock.Release();
        }

        try
        {
            // Reset on lease rather than on release: it is the only point that guarantees
            // a clean starting position even if a previous release failed halfway.
            await ResetSlotAsync(slot, cancellationToken);
        }
        catch
        {
            // Give the slot back rather than leaking it out of the pool.
            slot.SessionId = null;
            throw;
        }

        return slot;
    }

    private async Task<DemoSlot> ReclaimOldestSlotAsync(CancellationToken cancellationToken)
    {
        DemoSession? oldest = _sessions.Values.OrderBy(s => s.LastSeenAt).FirstOrDefault();

        if (oldest is null)
            throw new InvalidOperationException("Demo mode is not available right now.");

        logger.LogInformation("All demo slots are leased; reclaiming session {SessionId}.", oldest.Id);
        await EndAsync(oldest.Id, DemoSessionEndReason.SlotReclaimed, cancellationToken);

        return _slots.FirstOrDefault(s => s.IsFree)
               ?? throw new InvalidOperationException("Demo mode is not available right now.");
    }

    private async Task ResetSlotAsync(DemoSlot slot, CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Company? company = await context.Companies
            .FirstOrDefaultAsync(p => p.Id == slot.CompanyId, cancellationToken);

        if (company is null)
        {
            logger.LogError("Demo slot {Slot} has no company row; it cannot be reset.", slot.Index);
            return;
        }

        using var companyContext = new CompanyDbContext(company);
        await DemoDataSeeder.ResetAsync(companyContext, _demoUserId, cancellationToken);

        // The rows changed without going through the cache, so anything held for this
        // tenant is now wrong - the visitor would be shown the previous session's data
        // and get "not found" when acting on it.
        scope.ServiceProvider.GetRequiredService<ICacheService>()
            .RemoveTenant(slot.CompanyId.ToString());
    }

    private async Task<string> CreateTokenAsync(DemoSession session, CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        var jwtProvider = scope.ServiceProvider.GetRequiredService<IJwtProvider>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        AppUser user = await userManager.Users.FirstAsync(p => p.Id == session.UserId, cancellationToken);

        // Only the leased company is advertised, so the company switcher in the client
        // cannot be pointed at another visitor's sandbox.
        List<Company> companies =
        [
            new Company { Id = session.CompanyId, Name = session.CompanyName }
        ];

        return await jwtProvider.CreateTokenAsync(
            user,
            session.CompanyId,
            companies,
            extraClaims: new Dictionary<string, string> { [DemoClaimTypes.SessionId] = session.Id.ToString() },
            lifetime: TimeSpan.FromMinutes(_options.AbsoluteTimeoutMinutes + 5),
            cancellationToken: cancellationToken);
    }

    public async Task<bool> EndAsync(
        Guid sessionId,
        DemoSessionEndReason reason,
        CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryRemove(sessionId, out DemoSession? session)) return false;

        session.EndReason = reason;

        DemoSlot? slot = _slots.FirstOrDefault(s => s.SessionId == sessionId);
        if (slot is not null)
        {
            slot.SessionId = null;

            try
            {
                await WipeSlotAsync(slot, cancellationToken);
            }
            catch (Exception exception)
            {
                // The slot is reseeded on the next lease anyway, so a failed wipe must
                // not keep it out of the pool.
                logger.LogWarning(exception, "Demo slot {Slot} could not be wiped on release.", slot.Index);
            }
        }

        logger.LogInformation("Demo session {SessionId} ended ({Reason}).", sessionId, reason);

        return true;
    }

    private async Task WipeSlotAsync(DemoSlot slot, CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Company? company = await context.Companies
            .FirstOrDefaultAsync(p => p.Id == slot.CompanyId, cancellationToken);

        if (company is null) return;

        using var companyContext = new CompanyDbContext(company);
        await DemoDataSeeder.WipeAsync(companyContext, cancellationToken);

        scope.ServiceProvider.GetRequiredService<ICacheService>()
            .RemoveTenant(slot.CompanyId.ToString());
    }

    #endregion

    #region quota

    public bool IsAlive(Guid sessionId) => _sessions.ContainsKey(sessionId);

    public void Touch(Guid sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out DemoSession? session))
            session.LastSeenAt = DateTimeOffset.Now;
    }

    public bool TryRegisterWrite(Guid sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out DemoSession? session)) return false;

        lock (session)
        {
            if (session.WriteCount >= _options.WriteLimit) return false;

            session.WriteCount++;
            session.LastSeenAt = DateTimeOffset.Now;
        }

        return true;
    }

    public DemoSessionStatus? GetStatus(Guid sessionId) =>
        _sessions.TryGetValue(sessionId, out DemoSession? session) ? BuildStatus(session) : null;

    private DemoSessionStatus BuildStatus(DemoSession session)
    {
        int secondsRemaining = (int)Math.Max(0, (session.ExpiresAt - DateTimeOffset.Now).TotalSeconds);

        return new DemoSessionStatus(
            SessionId: session.Id,
            CompanyName: session.CompanyName,
            WritesUsed: session.WriteCount,
            WriteLimit: _options.WriteLimit,
            NudgeAfterWrites: _options.NudgeAfterWrites,
            ExpiresAt: session.ExpiresAt,
            SecondsRemaining: secondsRemaining,
            // A deployment that leaves the variable empty still gets a usable link
            // rather than a button that opens nothing.
            ContactUrl: string.IsNullOrWhiteSpace(_options.ContactUrl)
                ? DemoOptions.DefaultContactUrl
                : _options.ContactUrl,
            IsActive: session.EndReason == DemoSessionEndReason.None,
            EndReason: session.EndReason == DemoSessionEndReason.None ? null : session.EndReason.ToString());
    }

    #endregion

    #region reclamation

    /// <summary>
    /// Ends every session that ran out of time, went idle, or has to make room because
    /// the process is over its memory budget. Driven by the janitor hosted service.
    /// </summary>
    public async Task ReclaimAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = DateTimeOffset.Now;
        TimeSpan idleTimeout = TimeSpan.FromMinutes(_options.IdleTimeoutMinutes);

        foreach (DemoSession session in _sessions.Values.ToList())
        {
            if (now >= session.ExpiresAt)
                await EndAsync(session.Id, DemoSessionEndReason.Expired, cancellationToken);
            else if (now - session.LastSeenAt >= idleTimeout)
                await EndAsync(session.Id, DemoSessionEndReason.Idle, cancellationToken);
        }

        await ReclaimForMemoryAsync(cancellationToken);
    }

    private async Task ReclaimForMemoryAsync(CancellationToken cancellationToken)
    {
        if (_options.MemoryThresholdMegabytes <= 0 || _sessions.IsEmpty) return;

        long thresholdBytes = (long)_options.MemoryThresholdMegabytes * 1024 * 1024;
        if (CurrentMemoryBytes() <= thresholdBytes) return;

        // Measure again after a collection: a spike the GC can absorb on its own is not
        // a reason to throw a visitor out mid-session.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        if (CurrentMemoryBytes() <= thresholdBytes) return;

        logger.LogWarning(
            "Memory budget of {Threshold} MB exceeded; releasing the least recently used demo sessions.",
            _options.MemoryThresholdMegabytes);

        foreach (DemoSession session in _sessions.Values.OrderBy(s => s.LastSeenAt).ToList())
        {
            await EndAsync(session.Id, DemoSessionEndReason.MemoryPressure, cancellationToken);

            GC.Collect();
            if (CurrentMemoryBytes() <= thresholdBytes) return;
        }
    }

    private static long CurrentMemoryBytes()
    {
        using Process process = Process.GetCurrentProcess();
        return process.WorkingSet64;
    }

    #endregion
}
