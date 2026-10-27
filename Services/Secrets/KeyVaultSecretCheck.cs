using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Dashboard_server.Services.Secrets;

public static class KeyVaultSecretCheck
{
    public static async Task<int> RunAsync()
    {
        var keyVaultUrl = Environment.GetEnvironmentVariable("KEY_VAULT_URL");
        var secretName = Environment.GetEnvironmentVariable("SECRET_NAME") ?? "TrainingSecret";
        var useManagedIdentity = string.Equals( // 환경 변수 읽고
            Environment.GetEnvironmentVariable("KEY_VAULT_USE_MANAGED_IDENTITY"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!Uri.TryCreate(keyVaultUrl, UriKind.Absolute, out var vaultUri) // 설정 검증해서
            || vaultUri.Scheme != Uri.UriSchemeHttps)
        {
            Console.Error.WriteLine("Configuration error: Set KEY_VAULT_URL to https://<vault-name>.vault.azure.net/.");
            return 2;
        }

        if (string.IsNullOrWhiteSpace(secretName))
        {
            Console.Error.WriteLine("Configuration error: Set SECRET_NAME to the name of an existing secret.");
            return 2;
        }

        try
        {
            // 로컬에서는 az login 계정을 쓰고, Azure 호스트에서는 환경 변수로 관리 ID를 선택합니다.
            TokenCredential credential = useManagedIdentity // 인증방식 정해주고
                ? new ManagedIdentityCredential()
                : new AzureCliCredential();
            var secretClient = new SecretClient(vaultUri, credential); // secret 읽고

            var secret = await secretClient.GetSecretAsync(secretName);
            Console.WriteLine(string.IsNullOrWhiteSpace(secret.Value.Value)
                ? "Secret was retrieved, but its value is empty."
                : "Key Vault secret retrieved successfully. The secret value was not displayed.");

            return 0;
        }
        catch (Exception exception) // 예외처리
        {
            var credentialName = useManagedIdentity ? "managed identity" : "Azure CLI account";
            Console.Error.WriteLine(
                $"Key Vault secret retrieval failed ({exception.GetType().Name}). Check {credentialName} authentication, RBAC permissions, vault URL, and secret name.");
            return 1;
        }
    }
}