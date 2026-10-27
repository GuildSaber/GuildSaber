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
    public async Task ExecuteAsync(SongDifficulty songDifficulty, CancellationToken token)
    {
        logger.LogInformation("Executing added pipeline for ranked map with SongDifficultyId {SongDifficultyId}",
            songDifficulty.Id);

        await foreach (var score in dbContext.Scores
                           .Where(x => x.SongDifficultyId == songDifficulty.Id)
                           .AsAsyncEnumerable()
                           .WithCancellation(token))
            await scoreAddOrUpdatePipeline.ExecuteAsync(score, token);

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

                await scoreAddOrUpdatePipeline.ExecuteAsync(abstractScore, token);
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

                await scoreAddOrUpdatePipeline.ExecuteAsync(abstractScore, token);
            }
        }

        logger.LogInformation("Completed added pipeline for ranked map with SongDifficultyId {SongDifficultyId}",
            songDifficulty.Id);
    }
}