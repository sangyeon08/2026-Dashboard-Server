using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;
using Dashboard_server.Repositories;

namespace Dashboard_server.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureBlobStorage(this IServiceCollection services, IConfiguration configuration)
    {
        // Singleton인 이유: BlobServiceClient가 thread-safe하고 만드는 비용(인증 등)도 있어서, 요청마다 새로 만들 필요 없음.
        services.AddSingleton<IManagedIdentityStorageBlobRepository>(_ => // DI! 직접 레포를 만드는게 아니라, .net이 대신 만들어줌.
        {
            string accountUrl = configuration["AzureStorage:BlobServiceUri"]
                ?? throw new InvalidOperationException("AzureStorage:BlobServiceUri 설정이 필요합니다.");
            string? miClientId = configuration["AzureStorage:ManagedIdentityClientId"];

            var options = new DefaultAzureCredentialOptions();
            if (!string.IsNullOrWhiteSpace(miClientId))
                options.ManagedIdentityClientId = miClientId;    // mi 단계에서 사용할 클.아 지정
            else
                options.ExcludeManagedIdentityCredential = true; // 개발 중이니까 제외!

            // Azure client 만들기. 접근할 storage랑, 인증정보랑, 여러가지 가지고 있음.
            var client = new BlobServiceClient(new Uri(accountUrl), new DefaultAzureCredential(options));
            // 해당 자격 증명으로 직접 접근
            return new ManagedIdentityStorageBlobRepository(client);
        }); // 인증 부분은 유튭다시 보고 한번 더 공부하기. 어렵네,,

        return services;
    }

    // Table Storage도 인증 방식은 Blob이랑 완전 똑같음
    public static IServiceCollection AddAzureTableStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(_ =>
        {
            string accountUrl = configuration["AzureStorage:TableServiceUri"]
                ?? throw new InvalidOperationException("AzureStorage:TableServiceUri 설정이 필요합니다.");
            string? miClientId = configuration["AzureStorage:ManagedIdentityClientId"];

            var options = new DefaultAzureCredentialOptions();
            if (!string.IsNullOrWhiteSpace(miClientId))
                options.ManagedIdentityClientId = miClientId;
            else
                options.ExcludeManagedIdentityCredential = true;

            return new TableServiceClient(new Uri(accountUrl), new DefaultAzureCredential(options));
        });

        services.AddSingleton<ISnapshotTableRepository, SnapshotTableRepository>();
        services.AddSingleton<ILogTableRepository, LogTableRepository>();

        return services;
    }
}
