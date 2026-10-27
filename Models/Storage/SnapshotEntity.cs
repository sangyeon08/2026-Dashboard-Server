using Azure;
using Azure.Data.Tables;

namespace Dashboard_server.Models.Storage;

// 스냅샷 메타데이터 한 줄. 이미지 자체는 blob에만 있고, 여긴 "찾고 거르는 데" 필요한 정보만 들고 있음.
// PartitionKey/RowKey는 ITableEntity가 강제하는 필수 키. 데이터 양이 적어서 그냥 제일 간단하게 잡음.
public class SnapshotEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "snapshot"; // 고정값 하나 -> 파티션 분산 같은 거 신경 안 씀
    public string RowKey { get; set; } = string.Empty; // blobName 그대로 재사용 (이미 유니크해서 따로 안 만듦)
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string BlobName { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty; // "auto" | "manual"
    public int ErrorCount { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
    public long Size { get; set; } // 업로드 시점에 이미 알고 있는 값이라 그냥 같이 저장 (목록 조회할 때 blob 속성 또 안 읽어도 되게)
    public string ContentType { get; set; } = string.Empty;
}
