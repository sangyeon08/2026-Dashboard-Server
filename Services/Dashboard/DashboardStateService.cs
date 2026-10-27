using Dashboard_server.Models.Dashboard;

namespace Dashboard_server.Services.Dashboard;


public class DashboardStateService : IDashboardStateService
{ // mockdata 만들어서 보내주기
    private readonly object _lock = new();
    private readonly Random _random = new();

    // 10초마다 상황 연출로 그래프가 팡 튀게.
    private static readonly TimeSpan SpikeInterval = TimeSpan.FromSeconds(10);
    private DateTime _lastSpikeAt = DateTime.UtcNow;

    private readonly GlobalStatus _globalStatus = new()
    {
        Uptime = 99.8,
        NormalCount = 10,
        WarningCount = 1,
        CriticalCount = 1,
    };

    private readonly List<RegionStat> _regionStats =
    [
        new() { Region = "한국", ErrorThreshold = 10, ErrorCount = 2 },
        new() { Region = "미국", ErrorThreshold = 25, ErrorCount = 7 },
        new() { Region = "유럽", ErrorThreshold = 15, ErrorCount = 4 },
        new() { Region = "일본", ErrorThreshold = 8, ErrorCount = 1 },
    ];

    private readonly List<AlertTrendEntry> _alertTrend = BuildInitialAlertTrend();

    private static List<AlertTrendEntry> BuildInitialAlertTrend()
    {
        return Enumerable.Range(0, 24).Select(hour =>
        {
            var isBusyHour = hour is >= 14 and <= 16; // 이 시간대에 몰리게 데이터 구성. 

            return new AlertTrendEntry
            {
                Time = $"{hour:D2}:00",
                Critical = isBusyHour ? 2 : 0,
                Warning = isBusyHour ? 3 : hour % 6 == 0 ? 1 : 0,
                Info = isBusyHour ? 2 : 1,
            };
        }).ToList();
    }

    private static double Clamp(double value, double min, double max) => Math.Min(max, Math.Max(min, value));
    private int RandomWalk(int previousValue, int delta, int min, int max)
    {
        var change = _random.Next(delta * 2 + 1) - delta;
        return (int)Clamp(previousValue + change, min, max);
    }

    public DashboardSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            return new DashboardSnapshot
            {
                GlobalStatus = new GlobalStatus
                {
                    Uptime = _globalStatus.Uptime,
                    NormalCount = _globalStatus.NormalCount,
                    WarningCount = _globalStatus.WarningCount,
                    CriticalCount = _globalStatus.CriticalCount,
                },
                RegionStats = _regionStats
                    .Select(stat => new RegionStat { Region = stat.Region, ErrorThreshold = stat.ErrorThreshold, ErrorCount = stat.ErrorCount })
                    .ToList(),
                AlertTrend = _alertTrend
                    .Select(entry => new AlertTrendEntry { Time = entry.Time, Critical = entry.Critical, Warning = entry.Warning, Info = entry.Info })
                    .ToList(),
            };
        }
    }

    public GlobalStatus AdvanceGlobalStatus()
    {
        lock (_lock)
        {
            _globalStatus.Uptime = Clamp(_globalStatus.Uptime + (_random.NextDouble() * 0.4 - 0.2), 95, 100);
            _globalStatus.WarningCount = RandomWalk(_globalStatus.WarningCount, 1, 0, 5);
            _globalStatus.CriticalCount = RandomWalk(_globalStatus.CriticalCount, 1, 0, 3);

            return new GlobalStatus
            {
                Uptime = _globalStatus.Uptime,
                NormalCount = _globalStatus.NormalCount,
                WarningCount = _globalStatus.WarningCount,
                CriticalCount = _globalStatus.CriticalCount,
            };
        }
    }

    public List<RegionStat> AdvanceRegionStats()
    {
        lock (_lock)
        {
            var dueForSpike = DateTime.UtcNow - _lastSpikeAt >= SpikeInterval;

            foreach (var stat in _regionStats)
            {
                // 평소엔 목표 허용치 안에서만 등락 -> 정상 범위를 벗어나지 않게.
                stat.ErrorCount = RandomWalk(stat.ErrorCount, 1, 0, stat.ErrorThreshold);

                if (dueForSpike)
                {
                    // 스파이크 때만 허용치를 넘겨서 장애 상황을 연출 -> 자동 스냅샷 트리거.
                    stat.ErrorCount = (int)Clamp(stat.ErrorCount + _random.Next(10, 21), 0, 50);
                }
            }

            if (dueForSpike)
            {
                _lastSpikeAt = DateTime.UtcNow;
            }

            return _regionStats
                .Select(stat => new RegionStat { Region = stat.Region, ErrorThreshold = stat.ErrorThreshold, ErrorCount = stat.ErrorCount })
                .ToList();
        }
    }

    public List<AlertTrendEntry> AdvanceAlertTrend()
    {
        lock (_lock)
        {
            var entry = _alertTrend[_random.Next(_alertTrend.Count)];
            var severity = _random.Next(3);

            switch (severity)
            {
                case 0:
                    entry.Critical = (int)Clamp(entry.Critical + 1, 0, 10);
                    break;
                case 1:
                    entry.Warning = (int)Clamp(entry.Warning + 1, 0, 10);
                    break;
                default:
                    entry.Info = (int)Clamp(entry.Info + 1, 0, 10);
                    break;
            }

            return _alertTrend
                .Select(item => new AlertTrendEntry { Time = item.Time, Critical = item.Critical, Warning = item.Warning, Info = item.Info })
                .ToList();
        }
    }
}
