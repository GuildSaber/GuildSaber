using GuildSaber.Api.Features.Scores.Pipelines;
using GuildSaber.Api.Features.Scores.Services;
using GuildSaber.Api.Features.Scores.Workers;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatLeader.Models;
using GuildSaber.Common.Services.ScoreSaber;
using GuildSaber.Common.Services.ScoreSaber.Models;
using GuildSaber.Common.Services.ScoreSaber.Models.Responses;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Mappers.BeatLeader;
using GuildSaber.Database.Models.Mappers.ScoreSaber;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.RankedMaps.Pipelines;

public class AddRankedMapPipeline(
    ServerDbContext dbContext,
    ScoreAddOrUpdatePipeline scoreAddOrUpdatePipeline,
    BeatLeaderApi beatLeaderApi,
    ScoreSaberApi scoreSaberApi,
    HmdResolverService hmdResolver,
    ControllerResolverService controllerResolver,
    ILogger<AddRankedMapPipeline> logger)
{
    private const int ScoreBatchSize = 128;

    public async Task ExecuteAsync(SongDifficulty songDifficulty, CancellationToken token)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Executing added pipeline for ranked map with SongDifficultyId {SongDifficultyId}",
                songDifficulty.Id);

        var cursor = 0;
        while (true)
        {
            var after = new ScoreId(cursor);
            var scores = await dbContext.Scores
                .Where(x => x.SongDifficultyId == songDifficulty.Id && x.Id > after)
                .OrderBy(x => x.Id)
                .Take(ScoreBatchSize)
                .ToArrayAsync(token);

            if (scores.Length == 0)
                break;

            foreach (var score in scores)
                await scoreAddOrUpdatePipeline.ExecuteAsync(score, token: token);

            cursor = scores[^1].Id.Value;
        }

        if (songDifficulty.BLLeaderboardId is not null)
        {
            var requestOption = new BeatLeaderApi.PaginatedRequestOptions<LeaderboardSortBy>
            {
                Page = 1,
                MaxPage = int.MaxValue,
                SortBy = LeaderboardSortBy.Rank,
                Order = Order.Asc,
                PageSize = 100
            };
            await foreach (var score in beatLeaderApi
                               .GetLeaderboardAsyncEnumerable(songDifficulty.BLLeaderboardId.Value, requestOption)
                               .SelectMany(x => x.Unwrap()?.Scores ?? [])
                               .WithCancellation(token))
            {
                if (!(await BeatLeaderScoreSyncWorker.GetPlayerIdAsync(score.PlayerId, dbContext, token))
                    .TryGetValue(out var playerId)) continue;

                var scoreStats = (await beatLeaderApi.GetScoreStatisticsAsync(score.Id))
                    .GetValueOrDefault()
                    .Map();

                var abstractScore = await score.MapAsync(playerId, songDifficulty.Id, scoreStats,
                    hmdResolver.ResolveHmdIdAsync, controllerResolver.ResolveControllerIdAsync, token);

                await scoreAddOrUpdatePipeline.ExecuteAsync(abstractScore, token: token);
            }
        }

        if (songDifficulty.SSLeaderboardId is not null)
        {
            var requestOptions = new ScoreSaberApi.PaginatedRequestOptions<LeaderboardScoresSortBy>
            {
                Page = 1,
                MaxPage = int.MaxValue,
                SortBy = LeaderboardScoresSortBy.Score,
                PageSize = 100,
                SortDirection = SortDirection.Desc
            };

            await foreach (var score in scoreSaberApi
                               .GetLeaderboardScores(songDifficulty.SSLeaderboardId.Value, requestOptions, token)
                               .SelectMany(x => x.Unwrap() ?? [])
                               .Where(x => x.PlayOutcome == PlayOutcome.Clear)
                               .WithCancellation(token))
            {
                if (await dbContext.Players
                        .Where(x => x.LinkedAccounts.ScoreSaberId == score.Player.Id)
                        .Select(x => x.Id)
                        .Cast<PlayerId?>()
                        .FirstOrDefaultAsync(token) is not { } playerId) continue;

                var abstractScore = await score.MapAsync(playerId, songDifficulty.Id,
                    hmdResolver.ResolveHmdIdAsync, controllerResolver.ResolveControllerIdAsync, token);

                await scoreAddOrUpdatePipeline.ExecuteAsync(abstractScore, token: token);
            }
        }

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Completed added pipeline for ranked map with SongDifficultyId {SongDifficultyId}",
                songDifficulty.Id);
    }
}