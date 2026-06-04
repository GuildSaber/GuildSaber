using GuildSaber.Api.Features.Scores.Pipelines;
using GuildSaber.Api.Features.Scores.Workers;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatLeader.Models;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Mappers.BeatLeader;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.RankedMaps.Pipelines;

public class AddRankedMapPipeline(
    ServerDbContext dbContext,
    ScoreAddOrUpdatePipeline scoreAddOrUpdatePipeline,
    BeatLeaderApi beatLeaderApi,
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

                var abstractScore = score.Map(playerId, songDifficulty.Id, scoreStats);
                await scoreAddOrUpdatePipeline.ExecuteAsync(abstractScore, token);
            }
        }

        logger.LogInformation("Completed added pipeline for ranked map with SongDifficultyId {SongDifficultyId}",
            songDifficulty.Id);
    }
}