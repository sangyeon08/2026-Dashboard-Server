namespace Dashboard_server.Models.Dashboard;

// 클라이언트가 그룹에 막 참여했을 때 한 번에 내려주는 초기 스냅샷
public class DashboardSnapshot
{
    public GlobalStatus GlobalStatus { get; set; } = new();
    public List<RegionStat> RegionStats { get; set; } = new();
    public List<AlertTrendEntry> AlertTrend { get; set; } = new();
}
