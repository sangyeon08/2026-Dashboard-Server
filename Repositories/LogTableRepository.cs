using Azure.Data.Tables;
using Dashboard_server.Models.Storage;

namespace Dashboard_server.Repositories;

public class LogTableRepository : ILogTableRepository
{
    private const string TableName = "Logs";
    private readonly TableClient _tableClient;

    public LogTableRepository(TableServiceClient tableServiceClient, ILogger<LogTableRepository> logger)
    {
        _tableClient = tableServiceClient.GetTableClient(TableName);
        try
        {
            _tableClient.CreateIfNotExists();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Table Storage에 접근할 수 없어 테이블 생성을 건너뜁니다.");
        }
    }

    public async Task AddAsync(LogEntity entity)
    {
        await _tableClient.AddEntityAsync(entity);
    }

    // PartitionKey로 필터링해서 그 날짜 파티션만 가져온 다음, 최신순으로 정렬. 파트장님이 파티션 키가 속도 빨라서 효율적이라고 하심.
    public async Task<List<LogEntity>> ListByDateAsync(string dateKey, int maxCount = 200)
    {
        var results = new List<LogEntity>();
        var filter = $"PartitionKey eq '{dateKey.Replace("'", "''")}'"; // 작은따옴표 이스케이프 (OData 쿼리 문법)

        await foreach (var entity in _tableClient.QueryAsync<LogEntity>(filter))
        {
            results.Add(entity);
        }

        return results.OrderByDescending(e => e.CreatedAt).Take(maxCount).ToList();
    }
}
