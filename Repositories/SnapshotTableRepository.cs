using Azure.Data.Tables;
using Dashboard_server.Models.Storage;

namespace Dashboard_server.Repositories;

// Table Storage 버전. Blob 레포랑 패턴 똑같이 감 -> DI로 클라이언트 하나 받아서 그걸로 다 함.
public class SnapshotTableRepository : ISnapshotTableRepository
{
    private const string TableName = "SnapshotEvents";
    private readonly TableClient _tableClient;

    public SnapshotTableRepository(TableServiceClient tableServiceClient)
    {
        _tableClient = tableServiceClient.GetTableClient(TableName);
        _tableClient.CreateIfNotExists(); // 포털에서 미리 안 만들어놔도 됨 -> 없으면 여기서 자동 생성.
    }

    public async Task AddAsync(SnapshotEntity entity)
    {
        await _tableClient.AddEntityAsync(entity);
    }

    // 다 가져온 다음 최신순 정렬. 데이터 양 적을 땐 이걸로 충분하겠지만 많아지면 바꿔야 할 듯..?
    public async Task<List<SnapshotEntity>> ListAsync()
    {
        var results = new List<SnapshotEntity>();
        await foreach (var entity in _tableClient.QueryAsync<SnapshotEntity>())
        {
            results.Add(entity);
        }

        return results.OrderByDescending(e => e.CapturedAt).ToList();
    }

    public async Task DeleteAsync(string partitionKey, string rowKey)
    {
        await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
    }
}
