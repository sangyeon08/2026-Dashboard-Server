using Dashboard_server.Extensions;
using Dashboard_server.Hubs;
using Dashboard_server.Services.Dashboard;
using Dashboard_server.Services.Logs;
using Dashboard_server.Services.Snapshots;
using Dashboard_server.Services.Secrets;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<HostOptions>(o => // 백그라운드 워커가 오류를 내도 서버가 죽지 않게!
    o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);


builder.Services.AddControllers();

builder.Services.AddAzureBlobStorage(builder.Configuration);
builder.Services.AddAzureTableStorage(builder.Configuration);

// SignalR Hub를 사용하기 위한 서비스 등록
builder.Services.AddSignalR();

// 대시보드 목상태 + 3초마다 값 브로드캐스팅
builder.Services.AddSingleton<IDashboardStateService, DashboardStateService>();
builder.Services.AddHostedService<DashboardBroadcastWorker>();

// mock 데이터를 로그로도 쌓아서, Table Storage 로그 확인 화면에서 눈으로 볼 수 있게.
builder.Services.AddHostedService<LogMockWorker>();

// 장애 스냅샷 업로드 속도 제한
builder.Services.AddSingleton<SnapshotCooldownTracker>();

// 그리고 이건 swagger를 위해
builder.Services.AddOpenApi();

var app = builder.Build();
await KeyVaultSecretCheck.RunAsync(); // Key Vault Secret 과제용

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapHub<DashboardHub>("/hubs/dashboard"); // SignalR Hub 엔드포인트 연결해주기
app.MapFallbackToFile("index.html"); // Vue Router 경로에서 새로고침해도 화면이 뜨게

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUi(options =>
    {
        options.DocumentPath = "/openapi/v1.json";
    });
}

app.Run();
