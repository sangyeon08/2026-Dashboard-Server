using Dashboard_server.Models.Storage;

namespace Dashboard_server.Repositories;

// Azure Blob Storage 접근을 인터페이스로 감싼거. connection string 대신 Managed Identity로 인증하는 버전이라 이름이 이렇게 김.
// 컨트롤러가 이 인터페이스만 알고 실제 구현(ManagedIdentitiyStorageBlobRepository)은 DI로 주입받음 -> 나중에 storage 종류 바뀌어도 컨트롤러는 안 건드려도 됨.
public interface IManagedIdentityStorageBlobRepository
{
    // 업로드. 컨테이너 없으면 만들고, byte 배열 그대로 스트림으로 쏨.
    Task UploadAsync(
        string containerName,
        string blobName,
        byte[] bytes,
        string? contentType = null,
        IDictionary<string, string>? metadata = null);

    // 컨테이너가 비공개(private)라서, 프론트에서 직접 접근하려면 시간제한 있는 서명 URL(SAS)이 필요함.
    Task<string?> GetSasUrlAsync(
        string containerName,
        string blobName,
        int expiryHours = 1);

    // 컨테이너 안 blob 전체 목록 + 각각 SAS URL까지 같이 만들어서 반환 (스냅샷 목록 화면에서 씀).
    Task<List<BlobFileInfo>> ListAsync(
        string containerName,
        int sasExpiryHours = 1);

    // byte로 직접 다운받는 것도 만들어는 놨는데, 지금 프론트는 SAS URL로 바로 접근해서 실제로는 안 쓰임.
    Task<byte[]?> GetBytesAsync(
        string containerName,
        string blobName);

    // 삭제도 마찬가지로 아직 어디서도 호출 안 함. 나중에 스냅샷 정리 기능 만들면 쓸듯.
    Task DeleteAsync(
        string containerName,
        string blobName);
}