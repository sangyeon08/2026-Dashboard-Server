using Dashboard_server.Models.Dashboard;

namespace Dashboard_server.Services.Dashboard;

public interface IDashboardStateService
{
    // 현재 상태를 그대로 스냅샷으로 반환 (그룹에 새로 참여한 클라이언트용)
    DashboardSnapshot GetSnapshot();

    // 각 영역의 값을 한 걸음 랜덤워크로 흔들고, 갱신된 값을 반환
    GlobalStatus AdvanceGlobalStatus();
    List<RegionStat> AdvanceRegionStats();
    List<AlertTrendEntry> AdvanceAlertTrend();
}
