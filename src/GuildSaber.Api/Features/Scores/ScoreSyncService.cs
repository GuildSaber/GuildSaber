using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatLeader.Models.Responses;
using GuildSaber.Database.Contexts.Server;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using static GuildSaber.Common.Services.BeatLeader.Models.Responses.SocketGeneralResponse;

namespace GuildSaber.Api.Features.Scores;

public class ScoreSyncService(
    BeatLeaderGeneralSocketStream beatLeaderGeneralSocketStream,
    IServiceProvider serviceProvider,
    ILogger<ScoreSyncService> logger) : BackgroundService
{
    private readonly TimeSpan _reconnectAfter = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Continuously listens to the BeatLeader general socket stream for score events and queues them for processing.
    /// </summary>
    /// <param name="stoppingToken">A cancellation token that can be used to stop the background service.</param>
    /// <remarks>
    /// This method creates the core pipeline for BeatLeader score synchronization:
    /// <list type="number">
    ///     <item>Establishes and maintains a WebSocket connection to the BeatLeader score stream</item>
    ///     <item>Filters incoming score events for players that exist in our database</item>
    ///     <item>
    ///         <description>
    ///         Enqueues appropriate background jobs for different types of score events:
    ///         <list type="bullet">
    ///             <item>Upload - When a score is initially submitted but not yet processed</item>
    ///             <item>Accepted - When a score has been processed and accepted</item>
    ///             <item>Rejected - When a score has been rejected while it was previously accepted</item>
    ///         </list>
    ///         </description>
    ///     </item>
    ///     <item>Automatically reconnects after disconnection with a defined delay</item>
    /// </list>
    /// The service ensures durability by automatically reconnecting when the connection is lost
    /// and by using Hangfire to queue score processing, which provides persistence and retry capabilities.
    /// </remarks>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

        do
        {
            await foreach (var result in beatLeaderGeneralSocketStream.WithCancellation(stoppingToken))
            {
                if (!result.TryGetValue(out var response, out var error))
                {
                    logger.LogError("WebSocket error: {Error}", error);
                    break;
                }

                if (!await PlayerExistsInDb(response.PlayerId, dbContext, stoppingToken))
                    continue;

                BackgroundJob.Enqueue<ScoreSyncHandler>(response switch
                {
                    Upload upload => handle => handle.HandleUploadedScoreAsync(upload.Score),
                    Accepted accepted => handle => handle.HandleAcceptedScoreAsync(accepted.Score),
                    Rejected rejected => handle => handle.HandleRejectedScoreAsync(rejected.Score),
                    _ => throw new InvalidOperationException(
                        $"Unknown message type received from BeatLeader: {response.GetType().Name}")
                });
            }

            await Task.Delay(_reconnectAfter, stoppingToken);
        } while (!stoppingToken.IsCancellationRequested);
    }

    /// <summary>
    /// Checks if a player exists in the database by their BeatLeader ID.
    /// </summary>
    /// <throws cref="ArgumentException">Thrown when the playerId is not a valid ulong.</throws>
    private static Task<bool> PlayerExistsInDb(string beatleaderId, ServerDbContext dbContext, CancellationToken token)
        => dbContext.Players
            .Where(x => x.LinkedAccounts.BeatLeaderId == ulong.Parse(beatleaderId))
            .AnyAsync(cancellationToken: token);
}

public sealed class ScoreSyncHandler(ServerDbContext dbContext)
{
    public Task HandleUploadedScoreAsync(UploadScoreResponse response)
        => Task.CompletedTask;

    public Task HandleAcceptedScoreAsync(AcceptedScoreResponse response)
        => Task.CompletedTask;

    public Task HandleRejectedScoreAsync(RejectedScoreResponse response)
        => Task.CompletedTask;
}