using Dashboard_server.Models.Storage;
using Dashboard_server.Repositories;
using Dashboard_server.Services.Dashboard;

namespace Dashboard_server.Services.Logs;

// 5초마다 대시보드 상태를 가져와서 로그를 남김!
public class LogMockWorker(
    IDashboardStateService dashboardStateService,
    ILogTableRepository logRepository) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var snapshot = dashboardStateService.GetSnapshot();
            var totalErrorCount = snapshot.RegionStats.Sum(stat => stat.ErrorCount);

            var level = snapshot.GlobalStatus.CriticalCount > 0
                ? "Error"
                : snapshot.GlobalStatus.WarningCount > 0
                    ? "Warning"
                    : "Info";

            var message = $"가동률 {snapshot.GlobalStatus.Uptime:F1}% · 경고 {snapshot.GlobalStatus.WarningCount}건 · " +
                          $"심각 {snapshot.GlobalStatus.CriticalCount}건 · 지역별 에러 합계 {totalErrorCount}건";

            var entity = LogEntity.Create(level, message, "DashboardMock");
            await logRepository.AddAsync(entity);
        }
    }
}
