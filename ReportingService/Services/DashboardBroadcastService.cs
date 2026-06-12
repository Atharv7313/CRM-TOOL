using Microsoft.AspNetCore.SignalR;
using ReportingService.Hubs;
using ReportingService.Interfaces;
using Shared.DTOs;

namespace ReportingService.Services;

public class DashboardBroadcastService(
    IServiceScopeFactory scopeFactory,
    IHubContext<ReportingHub, IReportingClient> hub,
    ILogger<DashboardBroadcastService> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IKpiReportRepository>();
                var dashboard = await repo.GetDashboardSummaryAsync();
                await hub.Clients.All.ReceiveDashboardUpdate(dashboard);

                // Push any critical alerts separately so UI can toast them
                foreach (var alert in dashboard.Alerts.Where(a =>
                    a.Level == KpiAlertLevel.Critical))
                    await hub.Clients.All.ReceiveAlert(alert);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Dashboard broadcast failed");
            }
            await Task.Delay(_interval, ct);
        }
    }
}