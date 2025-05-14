using Hangfire;

namespace GuildSaber.Api.Hangfire;

public class HangfireActivator(IServiceProvider serviceProvider) : JobActivator
{
    public override object ActivateJob(Type jobType) => serviceProvider.GetRequiredService(jobType);

    public override JobActivatorScope BeginScope(JobActivatorContext context)
        => new HangfireScope(serviceProvider.CreateScope());
}

public class HangfireScope(IServiceScope serviceScope) : JobActivatorScope
{
    public override object? Resolve(Type type) => serviceScope.ServiceProvider.GetService(type);

    public override void DisposeScope() => serviceScope.Dispose();
}