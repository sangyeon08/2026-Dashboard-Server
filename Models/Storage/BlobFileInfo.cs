namespace Dashboard_server.Models.Storage;

// 컨테이너 안 blob 하나에 대한 정보 (목록 조회 결과)
public class BlobFileInfo
{
    public string BlobName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? ContentType { get; set; }
    public DateTimeOffset? CreatedOn { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
