namespace Dashboard_server.Models.Storage;

// POST /api/logs 요청 바디. 프론트/다른 서비스가 로그 한 줄을 남길 때 보내는 최소 정보만 받음.
public class AddLogRequest
{
    public string Level { get; set; } = "Info";
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }
}
