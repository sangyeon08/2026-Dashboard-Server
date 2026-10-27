using Azure;
using Azure.Data.Tables;

namespace Dashboard_server.Models.Storage;

// 원래는 시간별로 나누려고 했는데 그러면 핫 파티션이라고 로그가 너무 많이 쌓임;;
// 그래서 하루 단위로..!
public class LogEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty; // yyyyMMdd 형식으로
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; } 
    public ETag ETag { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; 
    public DateTimeOffset CreatedAt { get; set; } 

    public static LogEntity Create(string level, string message, string source = "")
    {
        var now = DateTimeOffset.UtcNow;
        return new LogEntity
        {
            PartitionKey = now.ToString("yyyyMMdd"),
            RowKey = $"{now:yyyyMMddTHHmmssfffZ}_{Guid.NewGuid():N}",
            Level = level,
            Message = message,
            Source = source,
            CreatedAt = now,
        };
    }
}
