using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Features.Players.Pipelines;
using GuildSaber.Api.Features.Scores.Pipelines;
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
    IServiceScopeFactory serviceScopeFactory,
    ILogger<BLScoreSyncWorker> logger)
    : BackgroundService
{
    private readonly TimeSpan _reconnectAfter = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Continuously listens to the BeatLeader general socket stream for score events and queues them for processing.
    /// </summary>
    /// <param name="token">A cancellation token that can be used to stop the background service.</param>
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
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
        var scoreAddOrUpdatePipeline = new ScoreAddOrUpdatePipeline(dbContext, new MemberPointStatsPipeline(dbContext));
        var memberPointStatsPipeline = new MemberPointStatsPipeline(dbContext);

        do
        {
            await foreach (var result in beatLeaderGeneralSocketStream.WithCancellation(token))
            {
                if (!result.TryGetValue(out var response, out var error))
                {
                    logger.LogError("WebSocket error: {Error}", error);
                    break;
                }

                if (!(await GetPlayerIdAsync(response.BeatLeaderId, dbContext, token))
                    .TryGetValue(out var playerId)) continue;

                if (!(await PlayerScoresPipeline.GetSongDifficultyIdAsync(response.LeaderboardId, dbContext, token))
                    .TryGetValue(out var difficultyId)) continue;

                var dbScore = response switch
                {
                    GeneralSocketMessage<UploadedScore>(var upload) => upload.Map(playerId, difficultyId),
                    GeneralSocketMessage<AcceptedScore>(var accepted) => accepted.Map(playerId, difficultyId,
                        (await beatLeaderApi.GetScoreStatisticsAsync(accepted.Id)).GetValueOrDefault().Map()),
                    GeneralSocketMessage<RejectedScore>(var rejected) => rejected.Map(playerId, difficultyId,
                        (await beatLeaderApi.GetScoreStatisticsAsync(rejected.Id)).GetValueOrDefault().Map()),
                    _ => throw new InvalidOperationException(
                        $"Unknown message type received from BeatLeader: {response.GetType().Name}")
                };

                var pipelineResult = await scoreAddOrUpdatePipeline.ExecuteAsync(dbScore);

                foreach (var context in pipelineResult.ImpactedContextsWithPoints)
                    await memberPointStatsPipeline.ExecuteAsync(playerId, context);
            }

            await Task.Delay(_reconnectAfter, token);
        } while (!token.IsCancellationRequested);
    }

    /// <summary>
    /// Retrieves the PlayerId for a given BeatLeader ID from the database.
    /// </summary>
    public static async Task<Maybe<PlayerId>> GetPlayerIdAsync(
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
}