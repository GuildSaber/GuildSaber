using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Members.ContextStats;
using GuildSaber.Api.Features.Guilds.Members.LevelStats;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Common.Result;
using GuildSaber.Database.Models.DiscordBot.FlexHistories;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.Guilds.Levels;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.DiscordBot.AutocompleteHandlers;
using GuildSaber.DiscordBot.Core.Handlers;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [SlashCommand("flex", "Show off your progress and claim roles!")]
    public async Task FlexAsync([Autocomplete<ContextAutocompleteHandler>] ContextId contextId)
    {
        await DeferAsync();

        var (playerTask, guildIdTask) = (GetPlayerAtMeAsync().AsTask(), GetGuildIdAsync().AsTask());
        await Task.WhenAll(playerTask, guildIdTask);

        var (player, guildId) = (playerTask.Result, guildIdTask.Result);
        var client = Client.Value;

        var (levelStatsTask, categoriesTask, contextStatsTask) = (
            client.LevelStats.GetByPlayerIdAsync(player.Id, contextId),
            client.Categories.GetAllByGuildIdAsync(guildId),
            client.ContextStats.GetByPlayerIdAsync(player.Id, contextId));
        await Task.WhenAll(levelStatsTask, categoriesTask, contextStatsTask);

        var (levelStats, categories, contextStats) = (
            levelStatsTask.Result.Unwrap(),
            categoriesTask.Result.Unwrap(),
            contextStatsTask.Result.Unwrap() ??
            throw new InteractionHandler.CurrentPlayerDidNotJoinGuildContextException()
        );

        var previousFlexHistory = await DbContext.FlexHistories
            .Where(x => x.PlayerId == player.Id && x.GuildId == guildId && x.ContextId == contextId)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefaultAsync();

        var flexHistory = FlexCommand.MakeNewFlexHistory(
            player.Id, guildId, contextId, DateTimeOffset.UtcNow, levelStats, contextStats);

        DbContext.FlexHistories.Add(flexHistory);
        await DbContext.SaveChangesAsync();

        // Cleanup old flex histories, keeping only the 20 most recent ones per player/guild/context to prevent unbounded growth of the table.
        await DbContext.FlexHistories
            .Where(x => x.PlayerId == player.Id && x.GuildId == guildId && x.ContextId == contextId)
            .OrderByDescending(x => x.Timestamp)
            .Skip(20)
            .ExecuteDeleteAsync();

        var requestOptions = new PaginatedRequestOptions<RankedMapRequests.ERankedMapSorter>
        {
            Page = 1,
            PageSize = 10,
            MaxPage = int.MaxValue,
            SortBy = RankedMapRequests.ERankedMapSorter.RankedScoreTime,
            Order = EOrder.Desc
        };

        List<RankedMapResponses.RankedMapWithScores> rankedMapsWithScores = [];
        await foreach (var rankedMapWithScore in Client.Value.RankedMaps.GetAsyncWithScoreAtMeEnumerable(
                               contextId, new RankedMapRequests.Filters(), requestOptions)
                           .SelectMany(x => x.Unwrap()))
        {
            if (rankedMapWithScore.RankedScores.All(x => x.Score.SetAt < previousFlexHistory?.Timestamp))
                break;

            rankedMapsWithScores.Add(rankedMapWithScore);
        }

        await FollowupAsync(components: FlexCommand
            .BuildFlexComponents(flexHistory, previousFlexHistory, rankedMapsWithScores));
    }
}

file static class FlexCommand
{
    public static MessageComponent BuildFlexComponents(
        FlexHistory flexHistory, FlexHistory? previousFlexHistory,
        List<RankedMapResponses.RankedMapWithScores> rankedMapsWithScores)
    {
        var builder = new ComponentBuilder();

        return builder.Build();
    }

    public static FlexHistory MakeNewFlexHistory(
        PlayerId playerId, GuildId guildId, ContextId contextId, DateTimeOffset dateTimeOffset,
        LevelStatResponses.MemberLevelStat[] levelStats,
        ContextStatResponses.MemberContextStat contextStats) => new()
    {
        GuildId = guildId,
        PlayerId = playerId,
        ContextId = contextId,
        Timestamp = dateTimeOffset,
        GlobalLevelId = levelStats
            .Where(x => x.Level.CategoryId is null && !x.IsLocked)
            .LastOrDefault(x => x.IsCompleted)?.Level?.Id is { } globalLevelId
            ? new Level.LevelId(globalLevelId)
            : null,
        LevelStats = levelStats
            .Where(x => x.Level.CategoryId is not null && !x.IsLocked)
            .GroupBy(x => x.Level.CategoryId!.Value)
            .Select(x => new FlexHistoryLevelStat
            {
                CategoryId = new Category.CategoryId(x.Key),
                LevelId = x.LastOrDefault(level => level.IsCompleted)?.Level.Id is { } levelId
                    ? new Level.LevelId(levelId)
                    : null
            }).ToArray(),
        PointStats = contextStats.SimplePointsWithRank.Where(x => x.CategoryId is null)
            .Select(x => new FlexHistoryPointStat
            {
                PointId = new Point.PointId(x.PointId),
                Rank = x.Rank,
                Points = x.Points
            }).ToArray()
    };
}