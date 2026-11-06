using Cronos;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GuildSaber.Api.Scheduling;

public static class CronJobExtensions
{
    public static IServiceCollection AddCronJob<TJob>(
        this IServiceCollection services, string expression, Func<TJob, CancellationToken, Task> execute,
        TimeZoneInfo? timeZone = null) where TJob : class
    {
        var schedule = CronExpression.Parse(expression, CronFormat.IncludeSeconds);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddTransient<TJob>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, CronJobScheduler>());
        services.AddSingleton(new CronJob(typeof(TJob).Name, schedule, timeZone ?? TimeZoneInfo.Local,
            (provider, token) => execute(provider.GetRequiredService<TJob>(), token)));

        return services;
    }
}
