# 2026-Dashboard-Server

ASP.NET Core 기반 대시보드 서버 (SignalR 실시간 전송, Azure Blob/Table Storage 연동)

## 설정
`appsettings.Development.json`의 `AzureStorage` 값을 본인 Azure Storage 계정에 맞게 수정합니다.

## 실행
```bash
dotnet run
```

## CI/CD (GitHub Actions)
- `ci.yml`: push / PR 시 빌드 확인
- `deploy.yml`: develop → staging, main → production 배포 (Azure Container Registry + App Service). 수동 실행(workflow_dispatch) 전용이라 자동으로 돌지 않으며, 파이프라인 구성 예시로 포함되어 있습니다.

저장소 Settings → Secrets and variables → Actions 에 아래 값을 등록해야 배포가 동작합니다.

| 종류 | 이름 |
| --- | --- |
| Secret | `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` |
| Variable | `ACR_NAME`, `RESOURCE_GROUP`, `STAGING_WEBAPP`, `PROD_WEBAPP` |

Settings → Environments 에서 `production` 환경에 Required reviewers를 지정하면 수동 승인 후 배포됩니다.
