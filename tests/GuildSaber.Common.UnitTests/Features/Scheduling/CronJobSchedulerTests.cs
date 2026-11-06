using System.Threading.Channels;
using AwesomeAssertions;
using GuildSaber.Api.Queuing;
using GuildSaber.Api.Scheduling;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;

namespace GuildSaber.Common.UnitTests.Features.Scheduling;

public class CronJobSchedulerTests
{
    private static readonly DateTimeOffset _midnight = new(2026, 11, 5, 0, 0, 0, TimeSpan.Zero);

    private sealed class Clock(DateTimeOffset start) : FakeTimeProvider(start)
    {
        public Channel<TimeSpan> Delays { get; } = Channel.CreateUnbounded<TimeSpan>();

        public override ITimer CreateTimer(
            TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = base.CreateTimer(callback, state, dueTime, period);
            Delays.Writer.TryWrite(dueTime);

            return timer;
        }
    }

    private sealed class Queue(int capacity = 10) : IHeavyBackgroundTaskQueue
    {
        private readonly HeavyBackgroundTaskQueue _queue = new(capacity);
        private int _enqueued;

        public int Enqueued => Volatile.Read(ref _enqueued);
        public Channel<bool> Attempts { get; } = Channel.CreateUnbounded<bool>();

        public async ValueTask QueueBackgroundWorkItemAsync(
            Func<CancellationToken, ValueTask> workItem, CancellationToken token = default)
        {
            Attempts.Writer.TryWrite(true);
            await _queue.QueueBackgroundWorkItemAsync(workItem, token);
            Interlocked.Increment(ref _enqueued);
        }

        public ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken token)
            => _queue.DequeueAsync(token);
    }

    private sealed class State
    {
        public List<(string Name, Scope Scope, CancellationToken Token)> Calls { get; } = [];
        public bool FailNext { get; set; }
    }

    private sealed class Scope : IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    [UsedImplicitly(Reason = "The scheduler resolves jobs through dependency injection.")]
    private sealed class Job(State state, Scope scope)
    {
        public Task ExecuteAsync(string name, CancellationToken token)
        {
            state.Calls.Add((name, scope, token));
            token.ThrowIfCancellationRequested();

            if (!state.FailNext)
                return Task.CompletedTask;

            state.FailNext = false;
            throw new InvalidOperationException("Scheduled test failure.");
        }
    }

    private static ServiceProvider Services(
        Clock clock, Queue queue, State state, params (string Expression, string Name)[] jobs)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<TimeProvider>(clock)
            .AddSingleton<IHeavyBackgroundTaskQueue>(queue)
            .AddSingleton(state)
            .AddScoped<Scope>();

        foreach (var (expression, name) in jobs)
            services.AddCronJob<Job>(expression, (job, token) => job.ExecuteAsync(name, token), TimeZoneInfo.Utc);

        return services.BuildServiceProvider();
    }

    [Test]
    public async Task DailyJobs_ShouldWaitForTheirTimesAndDisposeFreshScopes()
    {
        var clock = new Clock(_midnight);
        var queue = new Queue();
        var state = new State();
        await using var services = Services(clock, queue, state,
            ("0 0 5 * * *", "maps"), ("0 0 10 * * *", "confirmations"));
        var scheduler = services.GetServices<IHostedService>().OfType<CronJobScheduler>().Single();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await scheduler.StartAsync(timeout.Token);

        try
        {
            (await clock.Delays.Reader.ReadAsync(timeout.Token)).Should().Be(TimeSpan.FromHours(5));
            state.Calls.Should().BeEmpty();

            clock.Advance(TimeSpan.FromHours(5));
            var importMaps = await queue.DequeueAsync(timeout.Token);

            await importMaps(timeout.Token);

            state.Calls.Single().Name.Should().Be("maps");
            state.Calls.Single().Token.Should().Be(timeout.Token);
            state.Calls.Single().Scope.IsDisposed.Should().BeTrue();

            (await clock.Delays.Reader.ReadAsync(timeout.Token)).Should().Be(TimeSpan.FromHours(5));
            clock.Advance(TimeSpan.FromHours(5));
            var importConfirmations = await queue.DequeueAsync(timeout.Token);

            await importConfirmations(timeout.Token);

            state.Calls.Select(x => x.Name).Should().Equal("maps", "confirmations");
            state.Calls[1].Scope.Should().NotBeSameAs(state.Calls[0].Scope);
            state.Calls[1].Scope.IsDisposed.Should().BeTrue();
        }
        finally
        {
            await scheduler.StopAsync(timeout.Token);
        }
    }

    [Test]
    public async Task PendingRun_ShouldSkipDuplicateOccurrencesAndAllowTheNextRunAfterCompletion()
    {
        var clock = new Clock(_midnight);
        var queue = new Queue();
        var state = new State();
        await using var services = Services(clock, queue, state, ("* * * * * *", "import"));
        var scheduler = services.GetServices<IHostedService>().OfType<CronJobScheduler>().Single();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await scheduler.StartAsync(timeout.Token);

        try
        {
            await clock.Delays.Reader.ReadAsync(timeout.Token);
            clock.Advance(TimeSpan.FromSeconds(1));
            var first = await queue.DequeueAsync(timeout.Token);

            await clock.Delays.Reader.ReadAsync(timeout.Token);
            clock.Advance(TimeSpan.FromSeconds(1));
            await clock.Delays.Reader.ReadAsync(timeout.Token);

            queue.Enqueued.Should().Be(1);

            await first(timeout.Token);
            clock.Advance(TimeSpan.FromSeconds(1));
            var next = await queue.DequeueAsync(timeout.Token);

            await next(timeout.Token);

            state.Calls.Should().HaveCount(2);
        }
        finally
        {
            await scheduler.StopAsync(timeout.Token);
        }
    }

    [Test]
    public async Task FailedJob_ShouldDisposeItsScopeAndLeaveFutureRunsScheduled()
    {
        var clock = new Clock(_midnight);
        var queue = new Queue();
        var state = new State { FailNext = true };
        await using var services = Services(clock, queue, state, ("* * * * * *", "import"));
        var scheduler = services.GetServices<IHostedService>().OfType<CronJobScheduler>().Single();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await scheduler.StartAsync(timeout.Token);

        try
        {
            await clock.Delays.Reader.ReadAsync(timeout.Token);
            clock.Advance(TimeSpan.FromSeconds(1));
            var first = await queue.DequeueAsync(timeout.Token);

            await first(timeout.Token);

            state.Calls.Single().Scope.IsDisposed.Should().BeTrue();
            scheduler.ExecuteTask!.IsCompleted.Should().BeFalse();

            await clock.Delays.Reader.ReadAsync(timeout.Token);
            clock.Advance(TimeSpan.FromSeconds(1));
            var next = await queue.DequeueAsync(timeout.Token);

            await next(timeout.Token);

            state.Calls.Should().HaveCount(2);
            state.Calls[1].Scope.IsDisposed.Should().BeTrue();
        }
        finally
        {
            await scheduler.StopAsync(timeout.Token);
        }
    }

    [Test]
    public async Task Shutdown_ShouldCancelAWriteWaitingForHeavyQueueCapacity()
    {
        var clock = new Clock(_midnight);
        var queue = new Queue(capacity: 1);
        await queue.QueueBackgroundWorkItemAsync(static _ => ValueTask.CompletedTask);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await queue.Attempts.Reader.ReadAsync(timeout.Token);

        await using var services = Services(clock, queue, new State(), ("* * * * * *", "import"));
        var scheduler = services.GetServices<IHostedService>().OfType<CronJobScheduler>().Single();

        await scheduler.StartAsync(timeout.Token);

        try
        {
            await clock.Delays.Reader.ReadAsync(timeout.Token);
            clock.Advance(TimeSpan.FromSeconds(1));
            await queue.Attempts.Reader.ReadAsync(timeout.Token);

            await scheduler.StopAsync(timeout.Token);

            scheduler.ExecuteTask!.IsCompleted.Should().BeTrue();
            queue.Enqueued.Should().Be(1);
        }
        finally
        {
            await scheduler.StopAsync(timeout.Token);
        }
    }

    [Test]
    public async Task StartupAfterAnOccurrence_ShouldWaitForTheNextScheduledTime()
    {
        var clock = new Clock(_midnight.AddHours(7));
        var queue = new Queue();
        await using var services = Services(clock, queue, new State(),
            ("0 0 5 * * *", "maps"), ("0 0 10 * * *", "confirmations"));
        var scheduler = services.GetServices<IHostedService>().OfType<CronJobScheduler>().Single();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await scheduler.StartAsync(timeout.Token);

        try
        {
            (await clock.Delays.Reader.ReadAsync(timeout.Token)).Should().Be(TimeSpan.FromHours(3));
            queue.Enqueued.Should().Be(0);
        }
        finally
        {
            await scheduler.StopAsync(timeout.Token);
        }
    }

    [Test]
    public async Task DelayedWakeup_ShouldQueueEachDueJobOnceWithoutReplayingMissedOccurrences()
    {
        var clock = new Clock(_midnight);
        var queue = new Queue();
        var state = new State();
        await using var services = Services(clock, queue, state,
            ("* * * * * *", "maps"), ("* * * * * *", "confirmations"));
        var scheduler = services.GetServices<IHostedService>().OfType<CronJobScheduler>().Single();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await scheduler.StartAsync(timeout.Token);

        try
        {
            await clock.Delays.Reader.ReadAsync(timeout.Token);
            clock.Advance(TimeSpan.FromMinutes(5));

            var maps = await queue.DequeueAsync(timeout.Token);
            var confirmations = await queue.DequeueAsync(timeout.Token);

            await maps(timeout.Token);
            await confirmations(timeout.Token);

            (await clock.Delays.Reader.ReadAsync(timeout.Token)).Should().Be(TimeSpan.FromSeconds(1));
            queue.Enqueued.Should().Be(2);
            state.Calls.Select(x => x.Name).Should().Equal("maps", "confirmations");
        }
        finally
        {
            await scheduler.StopAsync(timeout.Token);
        }
    }

    [Test]
    public async Task DistantOccurrence_ShouldUseBoundedDelaysWithoutRunningEarly()
    {
        var clock = new Clock(_midnight);
        var queue = new Queue();
        await using var services = Services(clock, queue, new State(), ("0 0 0 1 1 *", "annual"));
        var scheduler = services.GetServices<IHostedService>().OfType<CronJobScheduler>().Single();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await scheduler.StartAsync(timeout.Token);

        try
        {
            (await clock.Delays.Reader.ReadAsync(timeout.Token)).Should().Be(TimeSpan.FromDays(1));
            clock.Advance(TimeSpan.FromDays(1));

            (await clock.Delays.Reader.ReadAsync(timeout.Token)).Should().Be(TimeSpan.FromDays(1));
            queue.Enqueued.Should().Be(0);
        }
        finally
        {
            await scheduler.StopAsync(timeout.Token);
        }
    }
}