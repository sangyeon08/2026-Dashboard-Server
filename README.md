# 2026-Dashboard-Server

**실시간 서비스 현황 대시보드의 백엔드**입니다. ASP.NET Core 기반으로, SignalR로 대시보드 데이터를 주기적으로 푸시하고 **Azure Blob / Table Storage**에 장애 스냅샷과 로그를 저장합니다.

화면은 [2026-Dashboard-Client](https://github.com/sangyeon08/2026-Dashboard-Client)가 담당합니다.

---

## 목차
1. [주요 기능](#주요-기능)
2. [기술 스택](#기술-스택)
3. [API](#api)
4. [SignalR Hub](#signalr-hub)
5. [데이터 저장 구조](#데이터-저장-구조)
6. [인증과 설정](#인증과-설정)
7. [백그라운드 작업](#백그라운드-작업)
8. [프로젝트 구조](#프로젝트-구조)
9. [CI/CD](#cicd)

---

## 주요 기능

| 기능 | 설명 |
| --- | --- |
| 실시간 푸시 | 가동률, 국가별 에러, 알림 추이를 3초마다 SignalR로 브로드캐스트 |
| 최초 스냅샷 | 클라이언트가 그룹에 참여하는 즉시 현재 상태 전체를 한 번에 전달 |
| 장애 스냅샷 저장 | 업로드된 이미지는 Blob에, 메타데이터는 Table에 분리 저장 |
| 로그 저장 / 조회 | 날짜 파티션 단위로 Table Storage에 기록하고 최신순으로 조회 |
| 업로드 제한 | 확장자 · 5MB 크기 제한, 서버 쿨다운으로 연속 업로드 차단 |
| Mock 데이터 | 실제 연동 전 단계라 가상의 상태를 생성하고, 10초마다 에러 스파이크를 연출 |

## 기술 스택

| 구분 | 사용 기술 |
| --- | --- |
| 프레임워크 | ASP.NET Core (.NET 10), Controllers |
| 실시간 통신 | SignalR |
| 저장소 | Azure Blob Storage, Azure Table Storage |
| 인증 | Azure.Identity (`DefaultAzureCredential`, Managed Identity) |
| 시크릿 | Azure Key Vault Secrets (접근 확인용 코드) |
| API 문서 | OpenAPI + NSwag Swagger UI (개발 환경) |
| 컨테이너 | Docker |

## API

| 메서드 | 경로 | 설명 |
| --- | --- | --- |
| `GET` | `/api/logs?date=yyyyMMdd&count=200` | 해당 날짜(기본: 오늘, UTC) 로그를 최신순으로 조회 |
| `POST` | `/api/logs` | 로그 한 건 기록 (`level`: Info / Warning / Error, `message`, `source`) |
| `GET` | `/api/snapshots` | 저장된 스냅샷 목록을 최신순으로 조회 (24시간 유효 SAS URL 포함) |
| `POST` | `/api/snapshots` | 스냅샷 업로드 (`multipart/form-data`: `file`, `trigger`, `errorCount`) |

### 스냅샷 업로드 규칙

| 항목 | 규칙 |
| --- | --- |
| 허용 확장자 | `.png`, `.jpg`, `.jpeg` |
| 최대 크기 | 5MB |
| 쿨다운 | 서버 기준 20초 안에 다시 요청하면 `429 Too Many Requests` |
| `trigger` | `auto` 외의 값은 모두 `manual`로 처리 |

쿨다운은 클라이언트에도 있지만, 탭이 여러 개이거나 새로고침하면 쉽게 초기화되므로 **서버가 최종 방어선** 역할을 합니다.

## SignalR Hub

엔드포인트: `/hubs/dashboard`

| 구분 | 이름 | 설명 |
| --- | --- | --- |
| 호출 | `JoinDashboardGroup` | 그룹에 참여하고, 호출한 클라이언트에게 현재 상태를 즉시 전송 |
| 호출 | `LeaveDashboardGroup` | 그룹에서 제거 |
| 전송 | `ReceiveDashboardSnapshot` | 참여 직후 1회, 전체 상태 (`globalStatus`, `regionStats`, `alertTrend`) |
| 전송 | `ReceiveGlobalStatus` | 가동률, 정상 / 경고 / 장애 수 |
| 전송 | `ReceiveRegionStats` | 국가별 허용 에러 수와 실제 에러 수 |
| 전송 | `ReceiveAlertTrend` | 시간대별(24시간) 심각도별 알림 수 |

## 데이터 저장 구조

이미지 원본과 검색용 정보를 나눠서 저장합니다.

| 저장소 | 이름 | 내용 |
| --- | --- | --- |
| Blob | `snapshots` 컨테이너 | 스냅샷 이미지 원본 (비공개) |
| Table | `SnapshotEvents` | 스냅샷 메타데이터 (`BlobName`, `Trigger`, `ErrorCount`, `CapturedAt`, `Size`, `ContentType`) |
| Table | `Logs` | 로그 (`PartitionKey`: `yyyyMMdd`) |

- **SAS URL은 저장하지 않습니다.** 만료되기 때문에 목록을 조회할 때마다 새로 발급합니다. (Managed Identity 기반 user delegation key 사용)
- Blob이 사라진 스냅샷은 목록 조회 시 Table에서도 정리합니다.
- 로그는 시간 단위로 파티션을 나누면 핫 파티션이 생길 수 있어서 **하루 단위 파티션**으로 저장합니다.
- 파일명은 `yyyyMMdd_HHmmss_{GUID}.확장자`라서 같은 초에 올려도 겹치지 않습니다.

## 인증과 설정

저장소 접근에 **연결 문자열이나 계정 키를 쓰지 않고**, `DefaultAzureCredential`로 Azure AD 인증을 사용합니다.

| 환경 | 인증 방식 |
| --- | --- |
| 로컬 개발 | 개발자 본인의 Azure 로그인 자격 증명 (Managed Identity 제외) |
| Azure 배포 | Managed Identity (`ManagedIdentityClientId`를 지정하면 사용자 할당 ID) |

### appsettings

```json
{
  "AzureStorage": {
    "BlobServiceUri": "https://<storage-account>.blob.core.windows.net/",
    "TableServiceUri": "https://<storage-account>.table.core.windows.net/",
    "ManagedIdentityClientId": ""
  }
}
```

### Key Vault 확인 코드
시작 시 `KeyVaultSecretCheck`가 환경 변수로 Key Vault 접근을 점검합니다. 시크릿 값은 출력하지 않습니다.

| 환경 변수 | 설명 |
| --- | --- |
| `KEY_VAULT_URL` | `https://<vault-name>.vault.azure.net/` |
| `SECRET_NAME` | 확인할 시크릿 이름 (기본: `TrainingSecret`) |
| `KEY_VAULT_USE_MANAGED_IDENTITY` | `true`면 Managed Identity, 아니면 Azure CLI 로그인 계정 사용 |

## 백그라운드 작업

| 작업 | 주기 | 역할 |
| --- | --- | --- |
| `DashboardBroadcastWorker` | 3초 | 상태를 갱신해 대시보드 그룹에 3종 이벤트 전송 |
| `LogMockWorker` | 5초 | 현재 상태를 요약해 로그로 기록 (가동률, 경고 / 심각 수, 지역별 에러 합계) |

- 두 작업에서 예외가 나도 서버가 종료되지 않도록 설정했습니다.
- `DashboardStateService`는 Mock 상태를 스레드 안전하게 관리합니다. 국가별 에러는 허용치 안에서 움직이다가, 10초마다 허용치를 넘는 스파이크를 만들어 **클라이언트의 자동 스냅샷이 동작하는 상황**을 재현합니다.

## 프로젝트 구조

```
Dashboard_server/
├── Program.cs                         서비스 등록, 미들웨어, 엔드포인트
├── Controllers/
│   ├── LogsController.cs              로그 기록 / 조회
│   └── SnapshotsController.cs         스냅샷 업로드 / 목록
├── Hubs/
│   └── DashboardHub.cs                SignalR Hub
├── Services/
│   ├── Dashboard/                     Mock 상태 관리, 3초 브로드캐스트
│   ├── Logs/                          5초 Mock 로그 생성
│   ├── Snapshots/                     업로드 쿨다운
│   └── Secrets/                       Key Vault 접근 확인
├── Repositories/                      Blob / Table 접근 계층 (인터페이스 + 구현)
├── Models/
│   ├── Dashboard/                     GlobalStatus · RegionStat · AlertTrendEntry · DashboardSnapshot
│   └── Storage/                       LogEntity · SnapshotEntity · 요청 / 응답 모델
├── Extensions/
│   └── ServiceCollectionExtensions.cs Azure 클라이언트 DI 등록
├── appsettings*.json
└── Dockerfile
```

> `wwwroot`에 클라이언트 빌드 결과를 넣으면 이 서버가 정적 파일로 함께 제공하고, Vue Router 경로로 새로고침해도 `index.html`로 연결됩니다.

## CI/CD

GitHub Actions 워크플로 2개가 있습니다.

| 파일 | 실행 시점 | 내용 |
| --- | --- | --- |
| `ci.yml` | `main` / `develop` push, PR | 복원 및 Release 빌드 확인 |
| `deploy.yml` | 수동 실행 (`workflow_dispatch`) | Azure Container Registry에서 이미지 빌드 후 App Service에 배포 (`develop` → staging, `main` → production) |

`deploy.yml`은 파이프라인 구성 예시로 수동 실행 전용입니다. 실제로 배포하려면 저장소의 Settings → Secrets and variables → Actions에 아래 값을 등록해야 합니다.

| 종류 | 이름 |
| --- | --- |
| Secret | `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` |
| Variable | `ACR_NAME`, `RESOURCE_GROUP`, `STAGING_WEBAPP`, `PROD_WEBAPP` |

Settings → Environments에서 `production`에 Required reviewers를 지정하면 승인 후에만 배포됩니다.
