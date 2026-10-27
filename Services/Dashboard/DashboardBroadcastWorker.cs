using Dashboard_server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Dashboard_server.Services.Dashboard;

// 3초마다 대시보드 상태 갱신해서 값 보내줌.
public class DashboardBroadcastWorker(
    IDashboardStateService dashboardStateService,
    IHubContext<DashboardHub> hubContext) : BackgroundService
{
    private static readonly TimeSpan BroadcastInterval = TimeSpan.FromSeconds(3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(BroadcastInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var group = hubContext.Clients.Group(DashboardHub.DASHBOARD_GROUP);

            // 카드랑 차트별로 나눠서 보내주기. 골라서 써!
            var globalStatus = dashboardStateService.AdvanceGlobalStatus();
            await group.SendAsync("ReceiveGlobalStatus", globalStatus, stoppingToken);

            var regionStats = dashboardStateService.AdvanceRegionStats();
            await group.SendAsync("ReceiveRegionStats", regionStats, stoppingToken);

            var alertTrend = dashboardStateService.AdvanceAlertTrend();
            await group.SendAsync("ReceiveAlertTrend", alertTrend, stoppingToken);
        }
    }
}
