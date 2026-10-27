using Dashboard_server.Services.Dashboard;
using Microsoft.AspNetCore.SignalR;

namespace Dashboard_server.Hubs;

public class DashboardHub(IDashboardStateService dashboardStateService) : Hub
{ // signalr hub, 클라이언트 관리!
    public const string DASHBOARD_GROUP = "DashboardGroup";

    public async Task JoinDashboardGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, DASHBOARD_GROUP);
        await Clients.Caller.SendAsync("ReceiveDashboardSnapshot", dashboardStateService.GetSnapshot());
        // 바로 현재 상태샷 보내줌.

    }

    public async Task LeaveDashboardGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, DASHBOARD_GROUP);
    }
}
