using System.Diagnostics;
using GuildSaber.Database.Contexts.DiscordBot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace GuildSaber.Migrator.DiscordBot;

public class Worker(
    IServiceProvider serviceProvider,
    ILogger<Worker> logger) : BackgroundService
{
    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource _activitySource = new(ActivitySourceName);

    public static bool IsFinished { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // ReSharper disable once ExplicitCallerInfoArgument
        using var activity = _activitySource.StartActivity("Migrating Discord Bot Database", ActivityKind.Client);

        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DiscordBotDbContext>();

            await EnsureDatabaseAsync(dbContext, stoppingToken);
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

    private static async Task EnsureDatabaseAsync(DiscordBotDbContext dbContext, CancellationToken cancellationToken)
        => await dbContext.Database.CreateExecutionStrategy()
            .ExecuteAsync(dbContext.GetService<IRelationalDatabaseCreator>(),
                static async (dbCreator, cancellationToken) =>
                {
                    if (!await dbCreator.ExistsAsync(cancellationToken)) await dbCreator.CreateAsync(cancellationToken);
                }, cancellationToken);

    private static async Task RunMigrationAsync(DiscordBotDbContext dbContext, CancellationToken cancellationToken)
        => await dbContext.Database.CreateExecutionStrategy()
            .ExecuteAsync(dbContext,
                static async (dbContext, cancellationToken) =>
                {
                    await dbContext.Database.MigrateAsync(cancellationToken);
                }, cancellationToken);

    private static async Task SeedDataAsync(DiscordBotDbContext dbContext, CancellationToken cancellationToken)
        => await dbContext.Database.CreateExecutionStrategy()
            .ExecuteAsync(dbContext, static async (dbContext, cancellationToken) =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);


                await transaction.CommitAsync(cancellationToken);
            }, cancellationToken);
}