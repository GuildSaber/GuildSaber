using GuildSaber.Api.Features.Scores;
using GuildSaber.Api.Features.Scores.Pipelines;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatLeader.Models;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Mappers.BeatLeader;

namespace GuildSaber.Api.Features.Players.Pipelines;

public sealed class PlayerScoresPipeline(
    ServerDbContext dbContext,
    BeatLeaderApi beatLeaderApi,
    ScoreAddOrUpdatePipeline addOrUpdatePipeline,
    ILogger<PlayerScoresPipeline> logger)
{
    public async Task ImportBeatLeaderScoresAsync(PlayerId playerId, BeatLeaderId beatLeaderId, CancellationToken token)
    {
        logger.LogInformation("Importing BeatLeader scores for player {PlayerId}", playerId);
        var initialRequest = new BeatLeaderApi.PaginatedRequestOptions<ScoresSortBy>
        {
            Page = 1,
            PageSize = 100,
            MaxPage = int.MaxValue,
            SortBy = ScoresSortBy.Date,
            Order = Order.Asc
        };

        // Unwrap the result to kill the current Task if there's an error.
        await foreach (var score in beatLeaderApi.GetPlayerScores(beatLeaderId, initialRequest)
                           .SelectMany(x => x.Unwrap() ?? [])
                           .WithCancellation(token))
        {
            var diffIdResult = await BLScoreSyncWorker.GetSongDifficultyIdAsync(
                score.LeaderboardId, dbContext, token);
            if (!diffIdResult.TryGetValue(out var difficultyId))
                continue;

            var scoreStats = (await beatLeaderApi.GetScoreStatisticsAsync(score.Id))
                .GetValueOrDefault()
                .Map();

            var abstractScore = score.Map(playerId, difficultyId, scoreStats);
            await addOrUpdatePipeline.ExecuteAsync(abstractScore);
        }

        logger.LogInformation("Completed importing BeatLeader scores for player {PlayerId}", playerId);
    }
}