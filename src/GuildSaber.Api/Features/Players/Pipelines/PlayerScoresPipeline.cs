using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Features.Scores.Pipelines;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatLeader.Models;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.Services.ScoreSaber;
using GuildSaber.Common.Services.ScoreSaber.Models;
using GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Mappers.BeatLeader;
using GuildSaber.Database.Models.Mappers.ScoreSaber;
using GuildSaber.Database.Models.Server.Guilds;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Players.Pipelines;

public sealed class PlayerScoresPipeline(
    ServerDbContext dbContext,
    BeatLeaderApi beatLeaderApi,
    ScoreSaberApi scoreSaberApi,
    ScoreAddOrUpdatePipeline addOrUpdatePipeline,
    MemberPointStatsPipeline memberPointStatsPipeline,
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

        var contextsWithPoints = new Dictionary<ContextId, Context>();

        // Unwrap the result to kill the current Task if there's an error.
        await foreach (var score in beatLeaderApi.GetPlayerScores(beatLeaderId, initialRequest)
                           .SelectMany(x => x.Unwrap() ?? [])
                           .WithCancellation(token))
        {
            if (!(await GetSongDifficultyIdAsync(score.LeaderboardId, dbContext, token))
                .TryGetValue(out var difficultyId)) continue;

            var scoreStats = (await beatLeaderApi.GetScoreStatisticsAsync(score.Id))
                .GetValueOrDefault()
                .Map();

            var abstractScore = score.Map(playerId, difficultyId, scoreStats);
            var pipelineResult = await addOrUpdatePipeline.ExecuteAsync(abstractScore);

            foreach (var context in pipelineResult.ImpactedContextsWithPoints)
                contextsWithPoints.TryAdd(context.Id, context);
        }

        foreach (var tuple in contextsWithPoints)
            await memberPointStatsPipeline.ExecuteAsync(playerId, tuple.Value);

        logger.LogInformation("Completed importing BeatLeader scores for player {PlayerId}", playerId);
    }

    public async Task ImportScoreSaberScoresAsync(PlayerId playerId, ScoreSaberId scoreSaberId, CancellationToken token)
    {
        logger.LogInformation("Importing ScoreSaber scores for player {PlayerId}", playerId);
        var initialRequest = new ScoreSaberApi.PaginatedRequestOptions<PlayerScoresSortBy>
        {
            Page = 1,
            PageSize = 100,
            MaxPage = int.MaxValue,
            SortBy = PlayerScoresSortBy.Recent
        };

        var contextsWithPoints = new Dictionary<ContextId, Context>();

        // Unwrap the result to kill the current Task if there's an error.
        await foreach (var playerScore in scoreSaberApi.GetPlayerScores(scoreSaberId, initialRequest)
                           .SelectMany(x => x.Unwrap() ?? [])
                           .WithCancellation(token))
        {
            if (!(await GetSongDifficultyIdAsync(playerScore.Leaderboard.Id, dbContext, token))
                .TryGetValue(out var difficultyId)) continue;

            var abstractScore = playerScore.Score.Map(playerId, difficultyId);
            var pipelineResult = await addOrUpdatePipeline.ExecuteAsync(abstractScore);

            foreach (var context in pipelineResult.ImpactedContextsWithPoints)
                contextsWithPoints.TryAdd(context.Id, context);
        }

        foreach (var tuple in contextsWithPoints)
            await memberPointStatsPipeline.ExecuteAsync(playerId, tuple.Value);
    }

    public static async Task<Maybe<SongDifficultyId>> GetSongDifficultyIdAsync(
        BLLeaderboardId leaderboardId, ServerDbContext dbContext, CancellationToken token)
        => await dbContext.SongDifficulties
                .Where(sd => sd.BLLeaderboardId == leaderboardId)
                .Select(sd => sd.Id)
                .Cast<SongDifficultyId?>()
                .FirstOrDefaultAsync(token) switch
            {
                null => None,
                var id => From(id.Value)
            };

    public static async Task<Maybe<SongDifficultyId>> GetSongDifficultyIdAsync(
        SSLeaderboardId leaderboardId, ServerDbContext dbContext, CancellationToken token)
        => await dbContext.SongDifficulties
                .Where(sd => sd.SSLeaderboardId == leaderboardId)
                .Select(sd => sd.Id)
                .Cast<SongDifficultyId?>()
                .FirstOrDefaultAsync(token) switch
            {
                null => None,
                var id => From(id.Value)
            };
}