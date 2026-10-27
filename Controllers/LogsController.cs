using Microsoft.AspNetCore.Mvc;
using Dashboard_server.Models.Storage;
using Dashboard_server.Repositories;

namespace Dashboard_server.Controllers;

[ApiController]
[Route("api/logs")]
public class LogsController : ControllerBase
{
    private static readonly string[] AllowedLevels = ["Info", "Warning", "Error"];

    private readonly ILogTableRepository _logRepository;

    public LogsController(ILogTableRepository logRepository)
    {
        _logRepository = logRepository;
    }

    [HttpPost]
    public async Task<IActionResult> AddLog([FromBody] AddLogRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("message는 필수입니다.");

        var level = AllowedLevels.Contains(request.Level) ? request.Level : "Info";

        var entity = LogEntity.Create(level, request.Message, request.Source ?? string.Empty);
        await _logRepository.AddAsync(entity);

        return Ok(new
        {
            partitionKey = entity.PartitionKey,
            rowKey = entity.RowKey,
            level = entity.Level,
            message = entity.Message,
            source = entity.Source,
            createdAt = entity.CreatedAt,
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] string? date, [FromQuery] int count = 200)
    {
        var dateKey = string.IsNullOrWhiteSpace(date) ? DateTimeOffset.UtcNow.ToString("yyyyMMdd") : date;

        var entities = await _logRepository.ListByDateAsync(dateKey, count);

        var result = entities.Select(e => new
        {
            partitionKey = e.PartitionKey,
            rowKey = e.RowKey,
            level = e.Level,
            message = e.Message,
            source = e.Source,
            createdAt = e.CreatedAt,
        });

        return Ok(result);
    }
}
