using eAccountingServer.Domain.Demo;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eAccountingServer.Infrastructure.Demo;

/// <summary>
/// Provisions the sandbox pool once at startup, then keeps it tidy: expired, idle and
/// memory-pressured sessions are handed back so an unattended public demo stays within
/// a fixed footprint.
/// </summary>
internal sealed class DemoHostedService(
    DemoSessionService demoSessionService,
    IOptions<DemoOptions> demoOptions,
    ILogger<DemoHostedService> logger
    ) : BackgroundService
{
    private readonly DemoOptions _options = demoOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Demo mode is disabled.");
            return;
        }

        try
        {
            await demoSessionService.InitializeAsync(stoppingToken);
        }
        catch (Exception exception)
        {
            // A failed provisioning run must not take the API down with it; the demo
            // endpoints report as unavailable until the process is restarted.
            logger.LogError(exception, "Demo provisioning failed.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(5, _options.JanitorIntervalSeconds));
        using PeriodicTimer timer = new(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await demoSessionService.ReclaimAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Demo session reclamation failed; retrying on the next tick.");
            }
        }
    }
}
