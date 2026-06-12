using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ReportingService.Interfaces;
using Shared.DTOs;

namespace ReportingService.Hubs;

[Authorize]
public class ReportingHub(IKpiReportRepository repo) : Hub<IReportingClient>
{
    /// <summary>
    /// Client calls this when it wants an immediate fresh dashboard.
    /// Server fetches from DB and responds only to that client — not everyone.
    /// </summary>
    public async Task RequestRefresh()
    {
        var dashboard = await repo.GetDashboardSummaryAsync();
        await Clients.Caller.ReceiveDashboardUpdate(dashboard);
    }
}

public interface IReportingClient
{
    /// <summary>Pushed to all clients every 30s by DashboardBroadcastService.</summary>
    Task ReceiveDashboardUpdate(DashboardSummaryDto dashboard);

    /// <summary>Pushed to all clients immediately when a new report is created.</summary>
    Task ReceiveNewReport(KpiReportDto report);

    /// <summary>Pushed to all clients when a KPI drops below threshold.</summary>
    Task ReceiveAlert(AlertDto alert);
}