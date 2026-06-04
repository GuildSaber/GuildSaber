using System.Reflection;
using GuildSaber.Api.Features.Auth.Settings;
using GuildSaber.Database.Contexts.Server;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.Customizer;
using TickerQ.EntityFrameworkCore.DependencyInjection;

namespace GuildSaber.Api.Setup;

public static class TickerQSetup
{
    public static WebApplicationBuilder AddTickerQ(this WebApplicationBuilder builder)
    {
        // https://github.com/Arcenox-co/TickerQ/issues/788
        if (Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider")
            return builder;

        var authSettings = builder.Configuration.GetSection(AuthSettings.AuthSettingsSectionKey);
        builder.Services.AddTickerQ(options =>
        {
            options.ConfigureScheduler(scheduler =>
            {
                scheduler.MaxConcurrency = 1;
                scheduler.NodeIdentifier = Environment.MachineName;
            });

            options.AddOperationalStore(efOptions =>
            {
                efOptions.UseApplicationDbContext<ServerDbContext>(ConfigurationType.IgnoreModelCustomizer);
                efOptions.SetDbContextPoolSize(34);
            });

            options.AddDashboard(dashboardOptionsBuilder => dashboardOptionsBuilder
                .WithApiKey(authSettings.GetSection(nameof(AuthSettings.TickerQ)).Get<TickerQAuthSettings>()!.ApiKey));
        });

        return builder;
    }
}