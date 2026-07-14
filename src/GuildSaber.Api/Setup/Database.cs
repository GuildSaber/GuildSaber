using GuildSaber.Database;
using GuildSaber.Database.Contexts.Server;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Setup;

public static class DatabaseSetup
{
    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<ServerDbContext>((_, options) =>
            options.UseNpgsql(builder.Configuration.GetConnectionString(Constants.ServerDbConnectionStringKey))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
                /* Allows methods (and mapping with ExpandableAttribute) to be translated to lambdas.
                 * This also makes .Compile() and .Invoke() crash in expressions when .AsExpandable isn't called.
                 * (Instead of allowing it to pull all the data instead of just what's needed)
                 * Using this then helps with Expression reusability and optimizes queries. */
                .WithExpressionExpanding());
        builder.EnrichNpgsqlDbContext<ServerDbContext>();

        return builder;
    }
}