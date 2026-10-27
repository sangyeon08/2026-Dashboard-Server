namespace Dashboard_server.Models.Dashboard;

// 국가별 목표 허용 에러 수 / 실제 에러 수
public class RegionStat
{
    public string Region { get; set; } = string.Empty;
    public int ErrorThreshold { get; set; }
    public int ErrorCount { get; set; }
}
