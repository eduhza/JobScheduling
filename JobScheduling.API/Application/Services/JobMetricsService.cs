using JobScheduling.API.Application.Services.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace JobScheduling.API.Application.Services;

public class JobMetricsService : IJobMetricsService
{
    private readonly ConcurrentDictionary<Guid, (DateTime scheduled, DateTime start, DateTime end)> _jobs = new();

    private long _insertCount;
    private readonly Stopwatch _insertStopwatch = new();

    public void StartInsertion() => _insertStopwatch.Start();
    public void FinishInsertion() => _insertStopwatch.Stop();

    public void IncrementInsertion() => Interlocked.Increment(ref _insertCount);

    public void RegisterExecution(Guid id, DateTime sched, DateTime start, DateTime end)
        => _jobs[id] = (sched, start, end);

    public MetricsSnapshot Snapshot()
    {
        var all = _jobs?.Values;
        var firstStart = all?.Min(j => j.start);
        var lastEnd = all?.Max(j => j.end);
        var totalExec = (lastEnd - firstStart)?.TotalSeconds;

        return new MetricsSnapshot(
            InsertTotal: _insertStopwatch.Elapsed.TotalSeconds,
            InsertAvg: _insertStopwatch.Elapsed.TotalSeconds / _insertCount,
            ExecTotal: totalExec ?? 0,
            ExecAvg: (totalExec / _jobs?.Count) ?? 0
        );
    }
}

public record MetricsSnapshot(double InsertTotal, double InsertAvg, double ExecTotal, double ExecAvg);
