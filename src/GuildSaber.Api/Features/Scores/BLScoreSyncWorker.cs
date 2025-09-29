using CSharpFunctionalExtensions;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatLeader.Models.Responses;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Mappers.BeatLeader;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Scores;

public class BLScoreSyncWorker(
    BeatLeaderApi beatLeaderApi,
    BeatLeaderGeneralSocketStream beatLeaderGeneralSocketStream,
    IServiceProvider serviceProvider,
    ILogger<BLScoreSyncWorker> logger) : BackgroundService
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

                var playerIdResponse = await GetPlayerIdAsync(response.BeatLeaderId, dbContext, stoppingToken);
                if (!playerIdResponse.TryGetValue(out var playerId))
                    continue;

                var diffIdResult = await GetSongDifficultyIdAsync(response.LeaderboardId, dbContext, stoppingToken);
                if (!diffIdResult.TryGetValue(out var difficultyId))
                    continue;

                var dbScore = response switch
                {
                    GeneralSocketMessage<UploadedScore>(var upload) => upload.Map(playerId, difficultyId),
                    GeneralSocketMessage<AcceptedScore>(var accepted) => accepted.Map((await beatLeaderApi
                        .GetScoreStatisticsAsync(accepted.Id)).GetValueOrDefault().Map(), playerId, difficultyId),
                    GeneralSocketMessage<RejectedScore>(var rejected) => rejected.Map((await beatLeaderApi
                        .GetScoreStatisticsAsync(rejected.Id)).GetValueOrDefault().Map(), playerId, difficultyId),
                    _ => throw new InvalidOperationException(
                        $"Unknown message type received from BeatLeader: {response.GetType().Name}")
                };

                /*BackgroundJob.Enqueue<ScoreService>(response is not SocketGeneralResponse.Rejected
                    ? handler => handler.AddScorePipelineAsync(dbScore)
                    : handler => handler.RemoveScorePipelineAsync(dbScore));*/
            }

            await Task.Delay(_reconnectAfter, stoppingToken);
        } while (!stoppingToken.IsCancellationRequested);
    }

    /// <summary>
    /// Retrieves the PlayerId for a given BeatLeader ID from the database.
    /// </summary>
    private static async Task<Maybe<PlayerId>> GetPlayerIdAsync(
        BeatLeaderId beatleaderId, ServerDbContext dbContext, CancellationToken token)
        => await dbContext.Players
                .Where(x => x.LinkedAccounts.BeatLeaderId == beatleaderId)
                .Select(x => x.Id)
                .Cast<PlayerId?>()
                .FirstOrDefaultAsync(token) switch
            {
                null => None,
                var id => From(id.Value)
            };

    private async Task<Maybe<SongDifficultyId>> GetSongDifficultyIdAsync(
        string leaderboardId, ServerDbContext dbContext, CancellationToken token)
        => await dbContext.SongDifficulties
                .Where(sd => sd.BLLeaderboardId == leaderboardId)
                .Select(sd => sd.Id)
                .Cast<SongDifficultyId?>()
                .FirstOrDefaultAsync(token) switch
            {
                null => None,
                var id => From(id.Value)
            };
}