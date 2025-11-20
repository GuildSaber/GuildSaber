using System.Diagnostics;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Migrator.Server.Seeders;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Migrator.Server;

public class Worker(IServiceProvider serviceProvider) : BackgroundService
{
    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource _activitySource = new(ActivitySourceName);

    public static bool IsFinished { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // ReSharper disable once ExplicitCallerInfoArgument
        using var activity = _activitySource.StartActivity("Migrating Guild Saber Database", ActivityKind.Client);

        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

            await RunMigrationAsync(dbContext, stoppingToken);
            await SeedDataAsync(dbContext, stoppingToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }
        finally
        {
            IsFinished = true;
        }
    }

    private static async Task RunMigrationAsync(ServerDbContext dbContext, CancellationToken cancellationToken)
        => await dbContext.Database.CreateExecutionStrategy()
            .ExecuteAsync(dbContext,
                static async (dbContext, cancellationToken) =>
                {
                    await dbContext.Database.MigrateAsync(cancellationToken);
                }, cancellationToken);

    private static async Task SeedDataAsync(ServerDbContext dbContext, CancellationToken cancellationToken)
        => await dbContext.Database.CreateExecutionStrategy()
            .ExecuteAsync(dbContext, static async (dbContext, cancellationToken) =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

                await GuildSeeder.SeedAsync(dbContext, cancellationToken);
                await PlayerSeeder.SeedAsync(dbContext, cancellationToken);
                await GameModeSeeder.SeedAsync(dbContext, cancellationToken);
                await PlayModeSeeder.SeedAsync(dbContext, cancellationToken);
                await ContextPointSeeder.SeedAsync(dbContext, cancellationToken);
                await LevelSeeder.SeedAsync(dbContext, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }, cancellationToken);
}