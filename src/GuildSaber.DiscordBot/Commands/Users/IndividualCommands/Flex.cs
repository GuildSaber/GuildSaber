using System.Text;
using Discord;
using Discord.Interactions;
using Discord.Net;
using GuildSaber.Api.Features.Guilds.Categories.Http;
using GuildSaber.Api.Features.Guilds.Levels.Http;
using GuildSaber.Api.Features.Guilds.Members.ContextStats.Http;
using GuildSaber.Api.Features.Guilds.Members.LevelStats.Http;
using GuildSaber.Api.Features.Players.Http;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Shared;
using GuildSaber.Common.Helpers;
using GuildSaber.Common.Result;
using GuildSaber.Database.Models.DiscordBot.FlexHistories;
using GuildSaber.Database.Models.Server.Guilds.Levels;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.StrongTypes;
using GuildSaber.DiscordBot.AutocompleteHandlers;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Core.Handlers;
using GuildSaber.DiscordBot.Settings;
using Microsoft.EntityFrameworkCore;
using static GuildSaber.Api.Features.RankedMaps.Http.RankedMapResponses;
using static GuildSaber.Api.Features.RankedScores.Http.RankedScoreResponses;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [SlashCommand("flex", "Show off your progress and claim roles!")]
    public async Task Flex([Autocomplete<ContextAutocompleteHandler>] ContextId contextId)
    {
        await DeferAsync();
        var client = Client.Value;

        var (player, guildId) = await (GetPlayerAsync().AsTask(), GetGuildIdAsync().AsTask())
            .WhenAll();

        var (levelStats, categories, contextStats) = await (
                client.LevelStats.GetByPlayerIdAsync(player.Id, contextId).Unwrap(),
                client.Categories.GetAllByGuildIdAsync(guildId).Unwrap(),
                client.ContextStats.GetByPlayerIdAsync(player.Id, contextId).Unwrap())
            .WhenAll();

        var previousFlexHistory = await DbContext.FlexHistories
            .Include(x => x.PointStats)
            .Include(x => x.LevelStats)
            .Where(x => x.PlayerId == player.Id && x.GuildId == guildId && x.ContextId == contextId)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefaultAsync();

        var flexHistory = FlexCommand.MakeNewFlexHistory(
            player.Id, guildId, contextId,
            DateTimeOffset.UtcNow,
            levelStats,
            contextStats ?? throw new InteractionHandler.CurrentPlayerDidNotJoinGuildContextException()
        );

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

        var flexComponent = FlexCommand.BuildFlexComponents(player, flexHistory, previousFlexHistory,
            rankedMapsWithScores,
            levelStats.Select(x => x.Level).ToDictionary(x => x.Id),
            categories,
            contextStats.Value.SimplePointsWithRank
                .Where(x => x.CategoryId == null)
                .ToDictionary(x => x.PointId, x => x.Name), EmojiSettings.Value,
            levelId => Client.Value.Levels.GetCoverUrl(levelId)
        );

        try
        {
            await FollowupAsync(components: flexComponent);
        }
        catch (HttpException httpException)
            when (httpException.DiscordCode == DiscordErrorCode.InvalidFormBody)
        {
            // Component is too big, split it into multiple messages.
            foreach (var component in flexComponent.Components)
                await FollowupAsync(components: new ComponentBuilderV2([component]).Build());
        }

        var (levelRoleIdsToAssign, levelRoleIdsToRemove) = (
            levelStats
                .Where(x => x.Level.CategoryId is null
                            && x is { IsLocked: false, IsCompleted: true, Level.DiscordInfo.RoleId: not null })
                .Select(x => (ulong)x.Level.DiscordInfo.RoleId!.Value)
                .Distinct()
                .ToArray(),
            levelStats.Where(x => x.Level.CategoryId is null
                                  && x is { IsCompleted: false, Level.DiscordInfo.RoleId: not null })
                .Select(x => (ulong)x.Level.DiscordInfo.RoleId!.Value)
                .Distinct()
                .ToArray()
        );

        var user = await ((IGuild)Context.Guild).GetUserAsync(Context.User.Id);
        var currentRoleIds = user.RoleIds.ToHashSet();
        var expectedRoleIds = currentRoleIds
            .Except(levelRoleIdsToRemove)
            .Union(levelRoleIdsToAssign)
            .ToHashSet();

        if (currentRoleIds.SetEquals(expectedRoleIds))
            return;

        try
        {
            await user.AddRolesAsync(levelRoleIdsToAssign);
            await user.RemoveRolesAsync(levelRoleIdsToRemove);
        }
        catch (Exception exception)
        {
            await FollowupAsync(
                $"Failed to update your roles.. {EmojiSettings.Value.Sad}\n" +
                $"Maybe they did delete a role without updating the level role ids? Ask the Ranking Team I guess.\n" +
                $":x: {exception.Message}");
            return;
        }

        await FollowupAsync("> Your roles have been updated!");
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
        EmojiSettings emojiSettings,
        Func<Level.LevelId, Uri> getLevelThumbnailUri)
    {
        var builder = new ComponentBuilderV2();
        var prevColor = previousFlexHistory is not null
            ? Color.FromArgb(levelsById[previousFlexHistory.GlobalLevelId?.Value ?? 0].Info.Color)
            : Color.Default;
        var newColor = flexHistory.GlobalLevelId is { } globalLevelId
            ? Color.FromArgb(levelsById[globalLevelId.Value].Info.Color)
            : Color.Default;

        var passedMapContainers = ToRankedScoreContainer(rankedMapsWithScores, levelsById, categories, pointNamesById,
            emojiSettings);
        foreach (var container in passedMapContainers)
            // Prev color is used to emphasize the level change during the flex.
            builder.WithContainer(container.WithAccentColor(prevColor));

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
            .WithAccentColor(prevColor));

        if (flexHistory.GlobalLevelId == previousFlexHistory?.GlobalLevelId)
            return builder.Build();

        var oldLevel = previousFlexHistory?.GlobalLevelId is { } oldLevelId ? levelsById[oldLevelId.Value] : null;
        var newLevel = flexHistory.GlobalLevelId is { } newLevelId ? levelsById[newLevelId.Value] : null;

        builder.WithContainer(content => content
            .WithSection(section =>
            {
                if (newLevel is not null)
                    section.WithAccessory(new ThumbnailBuilder()
                        .WithMedia(getLevelThumbnailUri(new Level.LevelId(newLevel.Id)).ToString()));

                section.WithTextDisplay((oldLevel, newLevel) switch
                {
                    (null, null) =>
                        $"## No levels yet!\n\u200B\nIt seems like you don't have any levels yet. Time to grind! {emojiSettings.KeepItUp}",
                    ({ } prevLevel, null) =>
                        $"## How unfortunate..\n\u200B\nIt seems like you lost **all your levels** since your last flex," +
                        $" from **{prevLevel.Info.Name}** to nothing. Don't be sad {emojiSettings.Sad}," +
                        $" it's just time to grind back up! {emojiSettings.KeepItUp}\n" +
                        "(To avoid level loss, try to play more maps in each level.)",
                    (null, { } level) =>
                        $"## Your got your first level!\n\u200B\nGG on reaching **{level.Info.Name}**!",
                    ({ } prevLevel, { } level) when prevLevel.Order < level.Order =>
                        $"## Level up!\n\u200B\nYou moved from **{prevLevel.Info.Name}** to **{level.Info.Name}**!",
                    ({ } prevLevel, { } level) when prevLevel.Order > level.Order =>
                        $"## Level down..\n\u200B\nIt seems like you lost some levels since your last flex," +
                        $" from **{prevLevel.Info.Name}** to **{level.Info.Name}**.\n" +
                        $"Don't be sad {emojiSettings.Sad}, you can do it!\n(To avoid level loss, try to play more maps in each level.)",
                    ({ } prevLevel, { } level) =>
                        $"## New level!\n\u200B\nIt seems like your level changed from **{prevLevel.Info.Name}**" +
                        $" to **{level.Info.Name}** since your last flex."
                });
            }).WithAccentColor(newColor));

        return builder.Build();
    }

    private static List<ContainerBuilder> ToRankedScoreContainer(
        List<RankedMapWithScores> rankedMapWithScores,
        Dictionary<int, LevelResponses.Level> levelsById,
        CategoryResponses.Category[] categories,
        Dictionary<int, string> pointNamesById,
        EmojiSettings emojiSettings)
    {
        var containers = new List<ContainerBuilder>();
        var (passedMaps, prohibitedMaps, pendingMaps, adminConfirmedMaps, adminRefusedMaps) =
        (
            rankedMapWithScores
                .Where(x => x.RankedScores.Any(y =>
                    !y.State.HasAnyFlag(EState.NonPointGiving)
                    && !y.State.HasFlag(EState.Confirmed)))
                .ToArray(),
            rankedMapWithScores
                .Where(x => x.RankedScores.Any(y =>
                    y.State.HasAnyFlag(EState.NonPointGivingNoPending)
                    && !y.State.HasFlag(EState.Refused)))
                .ToArray(),
            rankedMapWithScores
                .Where(x => x.RankedScores.Any(y => y.State.HasAnyFlag(EState.Pending)))
                .ToArray(),
            rankedMapWithScores
                .Where(x => x.RankedScores.Any(y => y.State.HasAnyFlag(EState.Confirmed)))
                .ToArray(),
            rankedMapWithScores
                .Where(x => x.RankedScores.Any(y => y.State.HasAnyFlag(EState.Refused)))
                .ToArray()
        );

        if (passedMaps.Length > 0)
            containers.Add(RankedScoreContainerBuilder(
                title: "### You passed the following maps:\n", passedMaps, levelsById, categories, pointNamesById,
                emojiSettings,
                take: 15));

        if (prohibitedMaps.Length > 0)
            containers.Add(RankedScoreContainerBuilder(
                title: "### You got scores on invalid states:\n", prohibitedMaps, levelsById, categories,
                pointNamesById,
                emojiSettings,
                take: 15));

        if (pendingMaps.Length > 0)
            containers.Add(RankedScoreContainerBuilder(
                title: "### You got scores that are pending review:\n", pendingMaps, levelsById, categories,
                pointNamesById,
                emojiSettings,
                take: 15));

        if (adminConfirmedMaps.Length > 0)
            containers.Add(RankedScoreContainerBuilder(
                title: "### Scores got admin confirmed:\n",
                adminConfirmedMaps, levelsById, categories,
                pointNamesById,
                emojiSettings,
                take: 15));

        if (adminRefusedMaps.Length > 0)
            containers.Add(RankedScoreContainerBuilder(
                title: "### Scores got admin refused:\n",
                adminRefusedMaps, levelsById, categories,
                pointNamesById,
                emojiSettings,
                take: 15));

        return containers;
    }

    private static ContainerBuilder RankedScoreContainerBuilder(
        string title, RankedMapWithScores[] rankedMapWithScores,
        Dictionary<int, LevelResponses.Level> levelsById,
        CategoryResponses.Category[] categories,
        Dictionary<int, string> pointNamesById,
        EmojiSettings emojiSettings,
        int take)
    {
        var stringBuilder = new StringBuilder(title);

        foreach (var rankedMap in rankedMapWithScores.Take(take))
        foreach (var rankedScore in rankedMap.RankedScores)
            stringBuilder.WriteRankedScores(rankedScore, rankedMap.RankedMap, levelsById, categories, pointNamesById,
                emojiSettings);

        if (rankedMapWithScores.Length > take)
            stringBuilder.AppendLine($"...and {rankedMapWithScores.Length - take} more.");

        return new ContainerBuilder().WithTextDisplay(stringBuilder.ToString());
    }

    private static void WriteRankedScores(
        this StringBuilder stringBuilder, RankedScore rankedScore,
        RankedMap rankedMap,
        Dictionary<int, LevelResponses.Level> levelsById,
        CategoryResponses.Category[] categories,
        Dictionary<int, string> pointNamesById,
        EmojiSettings emojiSettings)
    {
        var score = rankedScore.Score;
        var version = rankedMap.Versions.First(x => x.Difficulty.Id == score.SongDifficultyId);

        stringBuilder.Append(rankedScore.State switch
        {
            _ when rankedScore.State.HasAnyFlag(EState.NonPointGivingNoPending) => ":x: ",
            _ when rankedScore.State.HasAnyFlag(EState.Pending) => ":hourglass: ",
            _ when rankedScore.State.HasAnyFlag(EState.Confirmed) => emojiSettings.Confirmed,
            _ when rankedScore.State.HasAnyFlag(EState.Refused) => emojiSettings.Refused,
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
                CategoryId = new CategoryId(x.Key),
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