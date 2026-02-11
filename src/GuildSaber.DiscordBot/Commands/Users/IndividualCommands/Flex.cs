using System.Text;
using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Categories;
using GuildSaber.Api.Features.Guilds.Levels;
using GuildSaber.Api.Features.Guilds.Members.ContextStats;
using GuildSaber.Api.Features.Guilds.Members.LevelStats;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Api.Features.Players;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Common.Helpers;
using GuildSaber.Common.Result;
using GuildSaber.Database.Models.DiscordBot.FlexHistories;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.Guilds.Levels;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.StrongTypes;
using GuildSaber.DiscordBot.AutocompleteHandlers;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Core.Handlers;
using GuildSaber.DiscordBot.Settings;
using Microsoft.EntityFrameworkCore;
using static GuildSaber.Api.Features.RankedMaps.RankedMapResponses;
using static GuildSaber.Api.Features.RankedScores.RankedScoreResponses;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [SlashCommand("flex", "Show off your progress and claim roles!")]
    public async Task Flex([Autocomplete<ContextAutocompleteHandler>] ContextId contextId)
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
            .Include(x => x.PointStats)
            .Include(x => x.LevelStats)
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
            PageSize = 100,
            MaxPage = int.MaxValue,
            SortBy = RankedMapRequests.ERankedMapSorter.RankedScoreTime,
            Order = EOrder.Desc
        };

        List<RankedMapWithScores> rankedMapsWithScores = [];
        await foreach (var rankedMapWithScore in Client.Value.RankedMaps.GetAsyncWithScoreAtMeEnumerable(
                               contextId, new RankedMapRequests.Filters(), requestOptions)
                           .SelectMany(x => x.Unwrap()))
        {
            if (rankedMapWithScore.RankedScores.All(x => x.EditedAt < previousFlexHistory?.Timestamp))
                break;

            rankedMapsWithScores.Add(rankedMapWithScore);
        }

        await FollowupAsync(components: FlexCommand.BuildFlexComponents(player, flexHistory, previousFlexHistory,
            rankedMapsWithScores,
            levelStats.Select(x => x.Level).ToDictionary(x => x.Id),
            categories,
            contextStats.SimplePointsWithRank
                .Where(x => x.CategoryId == null)
                .ToDictionary(x => x.PointId, x => x.Name), EmojiSettings.Value)
        );
    }
}

file static class FlexCommand
{
    public static MessageComponent BuildFlexComponents(
        PlayerResponses.Player player, FlexHistory flexHistory,
        FlexHistory? previousFlexHistory,
        List<RankedMapWithScores> rankedMapsWithScores,
        Dictionary<int, LevelResponses.Level> levelsById,
        CategoryResponses.Category[] categories,
        Dictionary<int, string> pointNamesById,
        EmojiSettings emojiSettings)
    {
        var builder = new ComponentBuilderV2();
        var color = flexHistory.GlobalLevelId is { } globalLevelId
            ? Color.FromArgb(levelsById[globalLevelId.Value].Info.Color)
            : Color.Default;

        var passedMapContainers = ToRankedScoreContainer(rankedMapsWithScores, levelsById, categories, pointNamesById);
        foreach (var container in passedMapContainers)
            builder.WithContainer(container.WithAccentColor(color));

        var stringBuilder = new StringBuilder();
        stringBuilder.Append("### [")
            .Append(player.PlayerInfo.Username).Append("'s flex](https://beatleader.com/u/")
            .Append(player.PlayerLinkedAccounts.BeatLeaderId)
            .Append(")\n");

        var hasChanges = false;
        if (previousFlexHistory is not null)
            foreach (var pointStat in flexHistory.PointStats)
            {
                var oldPointStat = previousFlexHistory.PointStats.FirstOrDefault(x => x.PointId == pointStat.PointId);
                if (oldPointStat is null) continue;

                if (!(Math.Abs(pointStat.Points - oldPointStat.Points) > 0.01f) && pointStat.Rank == oldPointStat.Rank)
                    continue;

                var pointsDiff = pointStat.Points - oldPointStat.Points;
                var rankDiff = pointStat.Rank - oldPointStat.Rank;
                var pointsColor = pointsDiff >= 0 ? "[32m" : "[31m";
                var rankColor = rankDiff <= 0 ? "[32m" : "[31m";

                stringBuilder.AppendLine("```ansi")
                    .Append(' ').Append(pointStat.Points.ToString("0.##")).Append(' ')
                    .Append(pointNamesById[pointStat.PointId]).Append(" ").Append(pointsColor)
                    .Append(pointsDiff.ToString("+0.##;-0.##;0")).AppendLine("[0m")
                    .Append(" #").Append(pointStat.Rank).Append(" ").Append(rankColor)
                    .Append((-rankDiff).ToString("+#;-#;0")).AppendLine("[0m")
                    .AppendLine("```");

                hasChanges = true;
            }

        if (!hasChanges) stringBuilder.Append("\u200B\n");
        if (rankedMapsWithScores.Count > 0)
            stringBuilder.Append("> ").Append(emojiSettings.Congrats)
                .Append(" Congratulations <@").Append(player.PlayerLinkedAccounts.DiscordId).Append("> for those ")
                .Append(rankedMapsWithScores.Count).Append(" maps!");
        else
            stringBuilder.Append("> ").Append(emojiSettings.Sad)
                .Append(" No new maps found since your last flex. Time to grind! ").Append(emojiSettings.KeepItUp);

        builder.WithContainer(content => content
            .WithSection(section => section
                .WithTextDisplay(stringBuilder.ToString())
                .WithAccessory(new ThumbnailBuilder().WithMedia(player.PlayerInfo.AvatarUrl)))
            .WithAccentColor(color));

        return builder.Build();
    }

    public static List<ContainerBuilder> ToRankedScoreContainer(
        List<RankedMapWithScores> rankedMapWithScores,
        Dictionary<int, LevelResponses.Level> levelsById,
        CategoryResponses.Category[] categories,
        Dictionary<int, string> pointNamesById)
    {
        var containers = new List<ContainerBuilder>();
        var passedMaps = rankedMapWithScores
            .Where(x => x.RankedScores.Any(y => !y.State.HasAnyFlag(EState.NonPointGiving)))
            .ToArray();

        var prohibitedMaps = rankedMapWithScores
            .Where(x => x.RankedScores.Any(y => y.State.HasAnyFlag(EState.NonPointGivingNoPending)))
            .ToArray();

        var pendingMaps = rankedMapWithScores
            .Where(x => x.RankedScores.Any(y => y.State.HasAnyFlag(EState.Pending)))
            .ToArray();

        if (passedMaps.Length > 0)
            containers.Add(RankedScoreContainerBuilder(
                title: "### You passed the following maps:\n", passedMaps, levelsById, categories, pointNamesById,
                take: 10));

        if (prohibitedMaps.Length > 0)
            containers.Add(RankedScoreContainerBuilder(
                title: "### You got scores on invalid states:\n", prohibitedMaps, levelsById, categories,
                pointNamesById,
                take: 10));

        if (pendingMaps.Length > 0)
            containers.Add(RankedScoreContainerBuilder(
                title: "### You got scores that are pending review:\n", pendingMaps, levelsById, categories,
                pointNamesById,
                take: 10));

        return containers;
    }

    private static ContainerBuilder RankedScoreContainerBuilder(
        string title, RankedMapWithScores[] rankedMapWithScores,
        Dictionary<int, LevelResponses.Level> levelsById,
        CategoryResponses.Category[] categories,
        Dictionary<int, string> pointNamesById, int take)
    {
        var stringBuilder = new StringBuilder(title);

        foreach (var rankedMap in rankedMapWithScores.Take(take))
        foreach (var rankedScore in rankedMap.RankedScores)
            stringBuilder.WriteRankedScores(rankedScore, rankedMap.RankedMap, levelsById, categories, pointNamesById);

        if (rankedMapWithScores.Length > take)
            stringBuilder.AppendLine($"...and {rankedMapWithScores.Length - take} more.");

        return new ContainerBuilder().WithTextDisplay(stringBuilder.ToString());
    }

    private static void WriteRankedScores(
        this StringBuilder stringBuilder, RankedScore rankedScore,
        RankedMap rankedMap,
        Dictionary<int, LevelResponses.Level> levelsById,
        CategoryResponses.Category[] categories,
        Dictionary<int, string> pointNamesById)
    {
        var score = rankedScore.Score;
        var version = rankedMap.Versions.First(x => x.Difficulty.Id == score.SongDifficultyId);

        stringBuilder.Append(rankedScore.State switch
        {
            _ when rankedScore.State.HasAnyFlag(EState.NonPointGivingNoPending) => ":x: ",
            _ when rankedScore.State.HasAnyFlag(EState.Pending) => ":hourglass: ",
            _ when rankedScore.State.HasAnyFlag(EState.Selected) => ":white_check_mark: ",
            _ => string.Empty
        });
        stringBuilder.Append(((float)Accuracy.From(
                BaseScore.CreateUnsafe(score.BaseScore).Value,
                MaxScore.CreateUnsafe(version.Difficulty.Stats.MaxScore).Value))
            .ToString("N1")).Append("% ");

        if (rankedScore.PrevScore is { } prevScore)
        {
            var diff = score.BaseScore - prevScore.BaseScore;
            stringBuilder.Append(" (").Append(diff >= 0 ? "+" : "").Append(diff.ToString("N0")).Append(") ");
        }

        if (rankedScore.Score.Modifiers != RankedMapRequests.EModifiers.None)
            stringBuilder.Append(" | Mods: ").Append(rankedScore.Score.Modifiers).Append(' ');

        stringBuilder.Append("***`").Append(version.Difficulty.Difficulty);

        if (version.Difficulty.GameMode != "Standard")
            stringBuilder.Append(' ').Append(version.Difficulty.GameMode);

        stringBuilder.Append(" - ")
            .Append(version.Song.Info.Name.Replace("`", @"\`").Replace('*', ' ')).Append("`*** ");

        if (rankedMap.LevelIds.Length != 0)
        {
            stringBuilder.Append(" in **");
            foreach (var levelId in rankedMap.LevelIds)
            {
                var level = levelsById[levelId];
                if (level.CategoryId is not null) continue;

                stringBuilder.Append(level.Info.Name).Append(", ");
            }

            stringBuilder.Remove(stringBuilder.Length - 2, 2).Append("** ");
        }

        if (rankedMap.CategoryIds.Length != 0)
        {
            stringBuilder.Append(" **(");
            foreach (var categoryId in rankedMap.CategoryIds)
            {
                var category = categories.FirstOrDefault(x => x.Id == categoryId);
                if (category.Id == 0) continue;

                stringBuilder.Append(category.Info.Name).Append(", ");
            }

            stringBuilder.Remove(stringBuilder.Length - 2, 2).Append(")** ");
        }

        if (!rankedScore.State.HasAnyFlag(EState.NonPointGiving))
            stringBuilder.Append('(')
                .Append(rankedScore.RawPoints.ToString("0.##")).Append(' ').Append(pointNamesById[rankedScore.PointId])
                .Append(") ");

        stringBuilder.AppendLine(score is Score.BeatLeaderScore { BeatLeaderScoreId: { } blScoreId }
            ? $" [Replay](https://replay.beatleader.com/?scoreId={blScoreId})"
            : null);
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
            .LastOrDefault(x => x.IsCompleted)?.Level.Id is { } globalLevelId
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