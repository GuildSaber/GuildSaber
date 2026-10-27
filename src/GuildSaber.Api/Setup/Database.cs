using GuildSaber.Database;
using GuildSaber.Database.Contexts.Server;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Setup;

public static class DatabaseSetup
{
    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<ServerDbContext>((_, options) =>
            options.UseNpgsql(builder.Configuration.GetConnectionString(Constants.ServerDbConnectionStringKey),
                    //TODO: Remove this explicit retry configuration once the Aspire workaround below is no longer needed.
                    npgsql => npgsql.EnableRetryOnFailure())
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
                /* Allows methods (and mapping with ExpandableAttribute) to be translated to lambdas.
                 * This also makes .Compile() and .Invoke() crash in expressions when .AsExpandable isn't called.
                 * (Instead of allowing it to pull all the data instead of just what's needed)
                 * Using this then helps with Expression reusability and optimizes queries. */
                .WithExpressionExpanding());
        //TODO: Let Aspire configure retries again once it supports Npgsql 11.
        // Aspire 13.6.4 still references the removed NpgsqlExecutionStrategy type.
        builder.EnrichNpgsqlDbContext<ServerDbContext>(settings => settings.DisableRetry = true);

        return builder;
    }
}