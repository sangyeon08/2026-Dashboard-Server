using Dashboard_server.Models.Storage;

namespace Dashboard_server.Repositories;

// 스냅샷 메타데이터 테이블 접근용. Blob 레포랑 똑같은 이유로 인터페이스로 감쌈 (컨트롤러는 구현체 몰라도 됨).
public interface ISnapshotTableRepository
{
    Task AddAsync(SnapshotEntity entity);

    // 최신순 정렬까지 여기서 끝내고 돌려줌.
    Task<List<SnapshotEntity>> ListAsync();

    // blob은 지워졌는데 table 행만 남아있는 경우(수동으로 storage에서 지운 경우 등) 정리용.
    Task DeleteAsync(string partitionKey, string rowKey);
}
