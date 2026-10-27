namespace Dashboard_server.Models.Dashboard;

// 전체 서비스 가동률 / 정상·경고·장애 서버 수
public class GlobalStatus
{
    public double Uptime { get; set; }
    public int NormalCount { get; set; }
    public int WarningCount { get; set; }
    public int CriticalCount { get; set; }
}
