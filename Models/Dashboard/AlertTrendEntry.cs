namespace Dashboard_server.Models.Dashboard;

// 시간대별 알림 발생 추이 (심각도별)
public class AlertTrendEntry
{
    public string Time { get; set; } = string.Empty;
    public int Critical { get; set; }
    public int Warning { get; set; }
    public int Info { get; set; }
}
