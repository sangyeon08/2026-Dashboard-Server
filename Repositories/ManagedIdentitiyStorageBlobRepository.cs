using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Dashboard_server.Models.Storage;

namespace Dashboard_server.Repositories;

// 실제 구현체. connection string(계정 키)를 코드/설정에 안 박아두고, Azure AD 기반 Managed Identity로 인증함.
// -> 키 유출 걱정 없고, 로컬 개발할 땐 ServiceCollectionExtensions에서 내 Azure 로그인 자격증명으로 대체됨.
public class ManagedIdentityStorageBlobRepository : IManagedIdentityStorageBlobRepository
{
    private readonly BlobServiceClient _blobServiceClient; // readonly는 한번 정해지면 고정값!

    public ManagedIdentityStorageBlobRepository(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient; // 어색하게 생겼지만 이게 명명 규칙. _는 private field를 의미.
    }

    public async Task UploadAsync(string containerName, string blobName, byte[] bytes, string? contentType = null, IDictionary<string, string>? metadata = null)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None); // 없으면 만들어! 대신 비공개 처리.

        var blob = container.GetBlobClient(blobName); // 파일 이름 가져오기.
        using var stream = new MemoryStream(bytes); // using은 close() 역할. 소모품처럼!

        var options = new BlobUploadOptions();
        if (!string.IsNullOrEmpty(contentType))
        {
            options.HttpHeaders = new BlobHttpHeaders { ContentType = contentType };
        }
        if (metadata != null)
        {
            options.Metadata = metadata; // 스샷의 부가적 정보 저장용. 뭐 예를 들어 지역, 버전 같은거.
        }
        await blob.UploadAsync(stream, options); // 여기가 진짜 업로드.
    }

    // 최신순으로 보기. 단, 비용이 커서 키 하나에 다 가져오기.
    // (delegationKey를 blob 하나마다 새로 발급받으면 그 자체로 Azure 호출이라 느려짐 -> 루프 밖에서 한 번만 받고 재사용)
    public async Task<List<BlobFileInfo>> ListAsync(string containerName, int sasExpiryHours = 1)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        if (!await container.ExistsAsync())
            return [];

        // user delegation key = Managed Identity로 "나 대신 SAS 서명해도 된다"는 위임 키. 계정 키가 없어도 이걸로 SAS를 만들 수 있음.
        var delegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(
            new BlobGetUserDelegationKeyOptions(DateTimeOffset.UtcNow.AddHours(sasExpiryHours + 1))
            {
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5)
            });

        var results = new List<BlobFileInfo>();
        await foreach (var blobItem in container.GetBlobsAsync(new GetBlobsOptions { Traits = BlobTraits.Metadata }))
        {
            var blob = container.GetBlobClient(blobItem.Name);

            // blob 하나하나마다 읽기 전용(Read) 권한만 있는 SAS 쿼리 파라미터를 만드는 부분. GetSasUrlAsync랑 똑같은 패턴.
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobItem.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(sasExpiryHours)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var blobUriBuilder = new BlobUriBuilder(blob.Uri)
            {
                Sas = sasBuilder.ToSasQueryParameters(delegationKey, _blobServiceClient.AccountName)
            };

            results.Add(new BlobFileInfo
            {
                BlobName = blobItem.Name,
                Url = blobUriBuilder.ToUri().ToString(),
                Size = blobItem.Properties.ContentLength ?? 0,
                ContentType = blobItem.Properties.ContentType,
                CreatedOn = blobItem.Properties.CreatedOn,
                Metadata = blobItem.Metadata != null ? new Dictionary<string, string>(blobItem.Metadata) : [],
            });
        }

        return results.OrderByDescending(b => b.CreatedOn).ToList();
    }

    public async Task<string?> GetSasUrlAsync(string containerName, string blobName, int expiryHours = 1)
    {
        var blob = _blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName); // 보면 메서드를 이어서 작성함 -> 메서드 체이닝.

        if (!await blob.ExistsAsync())
            return null;

        var delegationKey = await _blobServiceClient.GetUserDelegationKeyAsync( // 유효기간 있는 권한 발급!
            new BlobGetUserDelegationKeyOptions(
                DateTimeOffset.UtcNow.AddHours(expiryHours + 1)) // 좀 더 연장해주기! 일단 기본 1시간.
            {
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5) // 왜 5분? -> 서버 시간차 때문에 예비로 함.
            });

        var sasBuilder = new BlobSasBuilder // sas에 들어갈 조건 정해주기.
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(expiryHours)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var blobUriBuilder = new BlobUriBuilder(blob.Uri)
        {
            Sas = sasBuilder.ToSasQueryParameters(delegationKey, _blobServiceClient.AccountName) // 앞에서 정한게 다 여기 들어감.
        };

        return blobUriBuilder.ToUri().ToString();
    }

    public async Task<byte[]?> GetBytesAsync(string containerName, string blobName) // byte 배열로 가져오기, 하지만 지금은 사용 안 함. 그냥 하나의 방식이다~ 알아두자.
    {
        var blob = _blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        if (!await blob.ExistsAsync())
            return null;

        using var ms = new MemoryStream();
        await blob.DownloadToAsync(ms);
        return ms.ToArray();
    }

    public async Task DeleteAsync(string containerName, string blobName) // 지금 사용 안 함.
    {
        var blob = _blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        await blob.DeleteIfExistsAsync();
    }
}