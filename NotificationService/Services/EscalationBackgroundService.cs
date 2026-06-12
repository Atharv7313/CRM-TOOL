using NotificationService.Interfaces;

namespace NotificationService.Services;

/// <summary>
/// Periodically scans for stale UNREAD escalatable notifications and escalates them.
/// Mirrors ReportingService.DashboardBroadcastService.
/// </summary>
public class EscalationBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<EscalationBackgroundService> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(
        config.GetValue<int?>("Notifications:EscalateScanSeconds") ?? 60);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var escalated = await svc.RunEscalationAsync();
                if (escalated > 0)
                    logger.LogInformation("Escalated {Count} notification(s)", escalated);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Escalation scan failed");
            }
            await Task.Delay(_interval, ct);
        }
    }
}
