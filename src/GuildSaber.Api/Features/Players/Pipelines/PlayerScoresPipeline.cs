using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Features.Scores.Pipelines;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatLeader.Models;
using GuildSaber.Common.Services.BeatLeader.Models.Responses;
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
    MemberLevelStatsPipeline memberLevelStatsPipeline,
    ILogger<PlayerScoresPipeline> logger)
{
    public async Task RecalculatePlayerScoresAsync(PlayerId playerId, CancellationToken token)
    {
        logger.LogInformation("Recalculating scores for player {PlayerId}", playerId);

        // Caches might not reflect latest changes, so we clear them first.
        await addOrUpdatePipeline.ClearPlayerCacheAsync(playerId);

        var count = 0;
        var contextsWithPoints = new Dictionary<ContextId, Context>();
        await foreach (var score in dbContext.Scores
                           .Where(s => s.PlayerId == playerId)
                           .AsAsyncEnumerable()
                           .WithCancellation(token))
        {
            var pipelineResult = await addOrUpdatePipeline.ExecuteAsync(score, token);

            count++;
            foreach (var context in pipelineResult.ImpactedContextsWithPoints)
                contextsWithPoints.TryAdd(context.Id, context);
        }

        foreach (var tuple in contextsWithPoints)
        {
            await memberPointStatsPipeline.ExecuteAsync(playerId, tuple.Value);
            foreach (var point in tuple.Value.Points)
                await memberLevelStatsPipeline.ExecuteAsync(playerId, tuple.Value.GuildId, tuple.Key, point.Id);
        }

        logger.LogInformation("Completed recalculating {count} scores for player {PlayerId}", count, playerId);
    }

    /// <remarks>
    /// All player scores end up being imported/updated via the ScoreAddOrUpdatePipeline.
    /// Not just scores of maps that are already in the db, all of them.
    /// </remarks>
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

        var count = 0;
        var contextsWithPoints = new Dictionary<ContextId, Context>();

        const int parallelizedChunkSize = 30;
        var chunkedScoresToProcess = new List<(ScoreResponse, SongDifficultyId)>(parallelizedChunkSize);

        var toBeatLeaderScoreAsync = async (ScoreResponse score, SongDifficultyId songDifficultyId) =>
            score.Map(playerId, songDifficultyId, (await beatLeaderApi.GetScoreStatisticsAsync(score.Id))
                .GetValueOrDefault().Map());

        // Unwrap the result to kill the current Task if there's an error.
        await foreach (var scoreResponses in beatLeaderApi.GetPlayerScoresAsyncEnumerable(beatLeaderId, initialRequest)
                           .SelectMany(x => x.Unwrap() ?? [])
                           .Chunk(parallelizedChunkSize)
                           .WithCancellation(token))
        {
            chunkedScoresToProcess.Clear();
            foreach (var scoreResponse in scoreResponses)
            {
                if (!(await GetSongDifficultyIdAsync(scoreResponse.LeaderboardId, dbContext, token))
                    .TryGetValue(out var difficultyId)) continue;

                chunkedScoresToProcess.Add((scoreResponse, difficultyId));
            }

            var scores =
                await Task.WhenAll(chunkedScoresToProcess.Select(x => toBeatLeaderScoreAsync(x.Item1, x.Item2)));
            foreach (var score in scores)
            {
                var pipelineResult = await addOrUpdatePipeline.ExecuteAsync(score, token);

                count++;
                foreach (var context in pipelineResult.ImpactedContextsWithPoints)
                    contextsWithPoints.TryAdd(context.Id, context);
            }
        }

        foreach (var tuple in contextsWithPoints)
        {
            await memberPointStatsPipeline.ExecuteAsync(playerId, tuple.Value);
            foreach (var point in tuple.Value.Points)
                await memberLevelStatsPipeline.ExecuteAsync(playerId, tuple.Value.GuildId, tuple.Key, point.Id);
        }

        logger.LogInformation("Completed importing {count} BeatLeader scores for player {PlayerId}", count, playerId);
    }

    /// <remarks>
    /// All player scores end up being imported/updated via the ScoreAddOrUpdatePipeline.
    /// Not just scores of maps that are already in the db, all of them.
    /// </remarks>
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

        var count = 0;
        var contextsWithPoints = new Dictionary<ContextId, Context>();

        // Unwrap the result to kill the current Task if there's an error.
        await foreach (var playerScore in scoreSaberApi.GetPlayerScores(scoreSaberId, initialRequest)
                           .SelectMany(x => x.Unwrap() ?? [])
                           .WithCancellation(token))
        {
            if (!(await GetSongDifficultyIdAsync(playerScore.Leaderboard.Id, dbContext, token))
                .TryGetValue(out var difficultyId)) continue;

            var abstractScore = playerScore.Score.Map(playerId, difficultyId);
            var pipelineResult = await addOrUpdatePipeline.ExecuteAsync(abstractScore, token);

            count++;
            foreach (var context in pipelineResult.ImpactedContextsWithPoints)
                contextsWithPoints.TryAdd(context.Id, context);
        }

        foreach (var tuple in contextsWithPoints)
        {
            await memberPointStatsPipeline.ExecuteAsync(playerId, tuple.Value);
            foreach (var point in tuple.Value.Points)
                await memberLevelStatsPipeline.ExecuteAsync(playerId, tuple.Value.GuildId, tuple.Key, point.Id);
        }

        logger.LogInformation("Completed importing {count} ScoreSaber scores for player {PlayerId}", count, playerId);
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