using Cronos;
using GuildSaber.Api.Queuing;

namespace GuildSaber.Api.Scheduling;

internal sealed record CronJob(
    string Name,
    CronExpression Expression,
    TimeZoneInfo TimeZone,
    Func<IServiceProvider, CancellationToken, Task> ExecuteAsync);

internal sealed class CronJobScheduler(
    IEnumerable<CronJob> jobs,
    TimeProvider clock,
    IHeavyBackgroundTaskQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<CronJobScheduler> logger) : BackgroundService
{
    private static readonly TimeSpan _maximumDelay = TimeSpan.FromDays(1);

    private sealed class ScheduledJob(CronJob job, DateTimeOffset now)
    {
        private int _pending;

        public CronJob Job { get; } = job;
        public DateTimeOffset? NextRun { get; set; } = job.Expression.GetNextOccurrence(now, job.TimeZone);

        public bool TryQueue() => Interlocked.CompareExchange(ref _pending, 1, 0) == 0;
        public void Complete() => Volatile.Write(ref _pending, 0);
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        var now = clock.GetUtcNow();
        var schedules = jobs
            .Select(job => new ScheduledJob(job, now))
            .ToArray();

        try
        {
            while (!token.IsCancellationRequested)
            {
                var nextRun = schedules.MinBy(x => x.NextRun ?? DateTimeOffset.MaxValue)?.NextRun;

                if (nextRun is null)
                    return;

                var delay = nextRun.Value - clock.GetUtcNow();

                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay > _maximumDelay ? _maximumDelay : delay, clock, token);
                    continue;
                }

                now = clock.GetUtcNow();

                foreach (var scheduled in schedules)
                {
                    if (scheduled.NextRun is not { } due || due > now)
                        continue;

                    // Skip elapsed occurrences rather than replaying a backlog after a delayed wakeup.
                    scheduled.NextRun = scheduled.Job.Expression.GetNextOccurrence(now, scheduled.Job.TimeZone);

                    await QueueAsync(scheduled, token);
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    private async ValueTask QueueAsync(ScheduledJob scheduled, CancellationToken token)
    {
        if (!scheduled.TryQueue())
            return;

        try
        {
            await queue.QueueBackgroundWorkItemAsync(async cancellationToken =>
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    await scheduled.Job.ExecuteAsync(scope.ServiceProvider, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Scheduled job {Job} failed.", scheduled.Job.Name);
                }
                finally
                {
                    scheduled.Complete();
                }
            }, token);
        }
        catch
        {
            scheduled.Complete();
            throw;
        }
    }
}
