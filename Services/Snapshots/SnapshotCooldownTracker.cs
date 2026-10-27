namespace Dashboard_server.Services.Snapshots;

// 장애 스냅샷이 너무 잦게 쌓이지 않도록 서버에서도 최소 업로드 간격을 강제한다.
// 프론트엔드 쿨다운은 탭이 여러 개거나 페이지가 새로고침되면 쉽게 리셋되므로, 이쪽이 최종 방어선.
public class SnapshotCooldownTracker
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(20);
    private readonly object _lock = new();
    private DateTime _lastAcceptedAt = DateTime.MinValue;

    public bool TryAcquire()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            if (now - _lastAcceptedAt < Cooldown)
                return false;

            _lastAcceptedAt = now;
            return true;
        }
    }
}
