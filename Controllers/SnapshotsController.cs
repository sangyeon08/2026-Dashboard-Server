using Microsoft.AspNetCore.Mvc;
using Dashboard_server.Models.Storage;
using Dashboard_server.Repositories;
using Dashboard_server.Services.Snapshots;

namespace Dashboard_server.Controllers;

// blob엔 이미지 원본만, table엔 trigger/errorCount 같은 메타데이터만.
[ApiController]
[Route("api/snapshots")]
public class SnapshotsController : ControllerBase
{
    private const string ContainerName = "snapshots";
    private static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private readonly IManagedIdentityStorageBlobRepository _blobRepository;
    private readonly ISnapshotTableRepository _tableRepository;
    private readonly SnapshotCooldownTracker _cooldownTracker;

    public SnapshotsController(
        IManagedIdentityStorageBlobRepository blobRepository,
        ISnapshotTableRepository tableRepository,
        SnapshotCooldownTracker cooldownTracker)
    {
        _blobRepository = blobRepository;
        _tableRepository = tableRepository;
        _cooldownTracker = cooldownTracker;
    }

    // 수동 캡처 버튼 / 트리거 됐을 때 자동으로 호출
    [HttpPost]
    public async Task<IActionResult> UploadSnapshot(IFormFile file, [FromForm] string trigger, [FromForm] int errorCount)
    {
        if (file == null || file.Length == 0)
            return BadRequest("파일 없음");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return BadRequest("허용되지 않은 파일 형식");

        if (file.Length > MaxFileSizeBytes)
            return BadRequest("파일 크기가 5MB를 초과");

        // 쿨다운을 해줘서 막 와라락 찍히는 사태 방지.
        if (!_cooldownTracker.TryAcquire())
            return StatusCode(StatusCodes.Status429TooManyRequests, "너무 잦은 스냅샷 요청입니다. 잠시 후 다시 시도해주세요."); // 429 요청이 너무 많다~~~~~~~~

        var capturedAt = DateTimeOffset.UtcNow;
        // 같은 초에 찍혀도 겹치지 않게 guid.
        var blobName = $"{capturedAt:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{extension}";

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);

        var normalizedTrigger = trigger == "auto" ? "auto" : "manual";

        await _blobRepository.UploadAsync(ContainerName, blobName, memoryStream.ToArray(), file.ContentType);
        await _tableRepository.AddAsync(new SnapshotEntity
        {
            RowKey = blobName,
            BlobName = blobName,
            Trigger = normalizedTrigger,
            ErrorCount = errorCount,
            CapturedAt = capturedAt,
            Size = file.Length,
            ContentType = file.ContentType,
        });

        var sasUrl = await _blobRepository.GetSasUrlAsync(ContainerName, blobName, expiryHours: 24);

        return Ok(new
        {
            blobUrl = sasUrl,
            fileName = blobName,
            trigger = normalizedTrigger,
            errorCount,
            capturedAt,
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetSnapshots()
    {
        var entities = await _tableRepository.ListAsync();

        // SAS URL은 미리 저장해두면 만료되니까, 보여줄 때마다 blobName으로 새로 발급.
        var result = new List<object>();
        foreach (var entity in entities)
        {
            var sasUrl = await _blobRepository.GetSasUrlAsync(ContainerName, entity.BlobName, expiryHours: 24);

            if (sasUrl == null)
            {
                await _tableRepository.DeleteAsync(entity.PartitionKey, entity.RowKey);
                continue;
            }

            result.Add(new
            {
                blobUrl = sasUrl,
                fileName = entity.BlobName,
                size = entity.Size,
                contentType = entity.ContentType,
                trigger = entity.Trigger,
                errorCount = entity.ErrorCount,
                capturedAt = entity.CapturedAt,
            });
        }

        return Ok(result);
    }
}
