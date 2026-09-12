using System.Diagnostics;
using System.Text;
using Discord;
using Discord.Interactions;
using Discord.Net;
using GuildSaber.Api.Features.Guilds.Achievements.Http;
using GuildSaber.Api.Features.Guilds.Categories.Http;
using GuildSaber.Api.Features.Guilds.Members.AchievementStats.Http;
using GuildSaber.Api.Features.Guilds.Members.ContextStats.Http;
using GuildSaber.Api.Features.Players.Http;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Shared;
using GuildSaber.Common.Helpers;
using GuildSaber.Common.Result;
using GuildSaber.Database.Models.DiscordBot.FlexHistories;
using GuildSaber.Database.Models.Server.Guilds.Achievements;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.StrongTypes;
using GuildSaber.DiscordBot.Core.AutocompleteHandlers;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Core.Handlers;
using GuildSaber.DiscordBot.Settings;
using Microsoft.EntityFrameworkCore;
using static GuildSaber.Api.Features.RankedMaps.Http.RankedMapResponses;
using static GuildSaber.Api.Features.Scores.Http.ScoreResponses;
using static GuildSaber.Api.Features.RankedScores.Http.RankedScoreResponses;
using static GuildSaber.Api.Features.RankedScores.Http.RankedScoreResponses.RankedScore;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [SlashCommand("flex", "Show off your progress and claim roles!")]
    public async Task Flex([Autocomplete<ContextAutocompleteHandler>] ContextId contextId)
    {
        var client = Client.Value;

        var (player, guildId) = await (
                DeferAsync(),
                GetPlayerAsync().AsTask(),
                GetGuildIdAsync().AsTask())
            .WhenAll();

        var (achievementStats, categories, contextStats) = await (
                client.AchievementStats.GetByPlayerIdAsync(player.Id, contextId).Unwrap(),
                client.Categories.GetAllByGuildIdAsync(guildId).Unwrap(),
                client.ContextStats.GetByPlayerIdAsync(player.Id, contextId).Unwrap())
            .WhenAll();

        var previousFlexHistory = await DbContext.FlexHistories
            .Include(x => x.PointStats)
            .Include(x => x.AchievementStats)
            .Where(x => x.PlayerId == player.Id && x.GuildId == guildId && x.ContextId == contextId)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefaultAsync();

        var flexHistory = FlexCommand.MakeNewFlexHistory(
            player.Id, guildId, contextId,
            DateTimeOffset.UtcNow,
            achievementStats,
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
        await foreach (var rankedMapWithScore in Client.Value.RankedMaps.GetAsyncWithScoresAtMeEnumerable(
                               contextId, new RankedMapRequests.Filters(), requestOptions)
                           .SelectMany(x => x.Unwrap()))
        {
            if (rankedMapWithScore.RankedScores.All(x => x.EditedAt < previousFlexHistory?.Timestamp))
                break;

            rankedMapsWithScores.Add(rankedMapWithScore);
        }

        var flexComponent = FlexCommand.BuildFlexComponents(player, flexHistory, previousFlexHistory,
            rankedMapsWithScores,
            achievementStats.Select(x => x.Achievement).ToDictionary(x => x.Id),
            categories,
            contextStats.Value.SimplePointsWithRank
                .Where(x => x.CategoryId == null)
                .ToDictionary(x => x.PointId, x => x.Name), EmojiSettings.Value,
            achievementId => Client.Value.Achievements.GetCoverUrl(achievementId)
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

        var (achievementRoleIdsToAssign, achievementRoleIdsToRemove) = (
            achievementStats
                .Where(x => x.Achievement.CategoryId is null
                            && x is
                            {
                                IsLocked: false, IsCompleted: true,
                                Achievement.DiscordBindings.RoleId: not null
                            })
                .Select(x => (ulong)x.Achievement.DiscordBindings.RoleId!.Value)
                .Distinct()
                .ToArray(),
            achievementStats.Where(x => x.Achievement.CategoryId is null
                                        && x is
                                        {
                                            IsCompleted: false,
                                            Achievement.DiscordBindings.RoleId: not null
                                        })
                .Select(x => (ulong)x.Achievement.DiscordBindings.RoleId!.Value)
                .Distinct()
                .ToArray()
        );

        var user = await ((IGuild)Context.Guild).GetUserAsync(Context.User.Id);
        var currentRoleIds = user.RoleIds.ToHashSet();
        var expectedRoleIds = currentRoleIds
            .Except(achievementRoleIdsToRemove)
            .Union(achievementRoleIdsToAssign)
            .ToHashSet();

        if (currentRoleIds.SetEquals(expectedRoleIds))
            return;

        try
        {
            await user.AddRolesAsync(achievementRoleIdsToAssign);
            await user.RemoveRolesAsync(achievementRoleIdsToRemove);
        }
        catch (Exception exception)
        {
            await FollowupAsync(
                $"Failed to update your roles.. {EmojiSettings.Value.Sad}\n" +
                $"Maybe they deleted a role without updating the level role bindings? Ask the Ranking Team.\n" +
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
        Dictionary<int, AchievementResponses.Achievement> achievementsById,
        CategoryResponses.Category[] categories,
        Dictionary<int, string> pointNamesById,
        EmojiSettings emojiSettings,
        Func<Achievement.AchievementId, Uri> getAchievementThumbnailUri)
    {
        var builder = new ComponentBuilderV2();
        var prevColor = previousFlexHistory is not null
            ? Color.FromArgb(
                achievementsById[previousFlexHistory.GlobalAchievementId?.Value ?? 0].Info.Color)
            : Color.Default;
        var newColor = flexHistory.GlobalAchievementId is { } globalAchievementId
            ? Color.FromArgb(achievementsById[globalAchievementId.Value].Info.Color)
            : Color.Default;

        var passedMapContainers = ToRankedScoreContainer(
            rankedMapsWithScores, categories, pointNamesById, emojiSettings);
        foreach (var container in passedMapContainers)
            // Prev color is used to emphasize the achievement change during the flex.
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

        if (flexHistory.GlobalAchievementId == previousFlexHistory?.GlobalAchievementId)
            return builder.Build();

        var oldAchievement = previousFlexHistory?.GlobalAchievementId is { } oldAchievementId
            ? achievementsById[oldAchievementId.Value]
            : null;
        var newAchievement = flexHistory.GlobalAchievementId is { } newAchievementId
            ? achievementsById[newAchievementId.Value]
            : null;

        builder.WithContainer(content => content
            .WithSection(section =>
            {
                if (newAchievement is not null)
                    section.WithAccessory(new ThumbnailBuilder()
                        .WithMedia(getAchievementThumbnailUri(
                            new Achievement.AchievementId(newAchievement.Id)).ToString()));

                section.WithTextDisplay((oldAchievement, newAchievement) switch
                {
                    (null, null) =>
                        $"## No levels yet!\n\u200B\nIt seems like you don't have any levels yet. Time to grind! {emojiSettings.KeepItUp}",
                    ({ } previousAchievement, null) =>
                        $"## How unfortunate..\n\u200B\nIt seems like you lost **all your levels** since your last flex," +
                        $" from **{previousAchievement.Info.Name}** to nothing. Don't be sad {emojiSettings.Sad}," +
                        $" it's just time to grind back up! {emojiSettings.KeepItUp}\n" +
                        "(To avoid level loss, try to play more maps for each level.)",
                    (null, { } achievement) =>
                        $"## Your first level!\n\u200B\nGG on reaching **{achievement.Info.Name}**!",
                    ({ Progression: AchievementResponses.AchievementProgression.Ordered previousProgression } previousAchievement,
                        { Progression: AchievementResponses.AchievementProgression.Ordered progression } achievement)
                        when previousProgression.Order < progression.Order =>
                        $"## Level up!\n\u200B\nYou moved from **{previousAchievement.Info.Name}** to **{achievement.Info.Name}**!",
                    ({ Progression: AchievementResponses.AchievementProgression.Ordered previousProgression } previousAchievement,
                        { Progression: AchievementResponses.AchievementProgression.Ordered progression } achievement)
                        when previousProgression.Order > progression.Order =>
                        $"## Level down..\n\u200B\nIt seems like you lost some levels since your last flex," +
                        $" from **{previousAchievement.Info.Name}** to **{achievement.Info.Name}**.\n" +
                        $"Don't be sad {emojiSettings.Sad}, you can do it!\n" +
                        "(To avoid level loss, try to play more maps for each level.)",
                    ({ } previousAchievement, { } achievement) =>
                        $"## New level!\n\u200B\nIt seems like your level changed from **{previousAchievement.Info.Name}**" +
                        $" to **{achievement.Info.Name}** since your last flex."
                });
            }).WithAccentColor(newColor));

        return builder.Build();
    }

    private static List<ContainerBuilder> ToRankedScoreContainer(
        List<RankedMapWithScores> rankedMapWithScores,
        CategoryResponses.Category[] categories,
        Dictionary<int, string> pointNamesById,
        EmojiSettings emojiSettings)
    {
        var containers = new List<ContainerBuilder>();
        var (passedMaps, prohibitedMaps, pendingMaps, adminConfirmedMaps, adminRefusedMaps) =
        (
            rankedMapWithScores
                .Where(x => x.RankedScores.Any(y => y is ValidRankedScore))
                .ToArray(),
            rankedMapWithScores
                .Where(x => x.RankedScores.Any(y => y is InvalidRankedScore))
                .ToArray(),
            rankedMapWithScores
                .Where(x => x.RankedScores.Any(y => y is PendingRankedScore))
                .ToArray(),
            rankedMapWithScores
                .Where(x => x.RankedScores.Any(y => y is AcceptedRankedScore))
                .ToArray(),
            rankedMapWithScores
                .Where(x => x.RankedScores.Any(y => y is RefusedRankedScore))
                .ToArray()
        );

        if (passedMaps.Length > 0)
            containers.Add(RankedScoreContainerBuilder(
                title: "### You passed the following maps:\n", passedMaps, categories, pointNamesById,
                emojiSettings,
                take: 15));

        if (prohibitedMaps.Length > 0)
            containers.Add(RankedScoreContainerBuilder(
                title: "### You got scores on invalid states:\n", prohibitedMaps, categories,
                pointNamesById,
                emojiSettings,
                take: 15));

        if (pendingMaps.Length > 0)
            containers.Add(RankedScoreContainerBuilder(
                title: "### You got scores that are pending review:\n", pendingMaps, categories,
                pointNamesById,
                emojiSettings,
                take: 15));

        if (adminConfirmedMaps.Length > 0)
            containers.Add(RankedScoreContainerBuilder(
                title: "### Scores got admin confirmed:\n",
                adminConfirmedMaps, categories,
                pointNamesById,
                emojiSettings,
                take: 15));

        if (adminRefusedMaps.Length > 0)
            containers.Add(RankedScoreContainerBuilder(
                title: "### Scores got admin refused:\n",
                adminRefusedMaps, categories,
                pointNamesById,
                emojiSettings,
                take: 15));

        return containers;
    }

    private static ContainerBuilder RankedScoreContainerBuilder(
        string title, RankedMapWithScores[] rankedMapWithScores,
        CategoryResponses.Category[] categories,
        Dictionary<int, string> pointNamesById,
        EmojiSettings emojiSettings,
        int take)
    {
        var stringBuilder = new StringBuilder(title);

        foreach (var rankedMap in rankedMapWithScores.Take(take))
        foreach (var rankedScore in rankedMap.RankedScores)
            stringBuilder.WriteRankedScores(rankedScore, rankedMap.RankedMap, categories, pointNamesById,
                emojiSettings);

        if (rankedMapWithScores.Length > take)
            stringBuilder.AppendLine($"...and {rankedMapWithScores.Length - take} more.");

        return new ContainerBuilder().WithTextDisplay(stringBuilder.ToString());
    }

    private static void WriteRankedScores(
        this StringBuilder stringBuilder, RankedScore rankedScore,
        RankedMap rankedMap,
        CategoryResponses.Category[] categories,
        Dictionary<int, string> pointNamesById,
        EmojiSettings emojiSettings)
    {
        var score = rankedScore.Score;
        var version = rankedMap.Versions.First(x => x.Difficulty.Id == score.SongDifficultyId);

        stringBuilder.Append(rankedScore switch
        {
            InvalidRankedScore => ":x: ",
            PendingRankedScore => ":hourglass: ",
            AcceptedRankedScore => emojiSettings.Confirmed,
            RefusedRankedScore => emojiSettings.Refused,
            ValidRankedScore => ":white_check_mark: ",
            _ => throw new UnreachableException()
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

        var globalAchievements = rankedMap.Achievements.Where(x => x.CategoryId is null).ToArray();
        if (globalAchievements.Length != 0)
        {
            stringBuilder.Append(" in **");
            foreach (var achievement in globalAchievements) stringBuilder.Append(achievement.Info.Name).Append(", ");

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

        var pointGivingRawPoints = rankedScore switch
        {
            ValidRankedScore validRankedScore => validRankedScore.RawPoints,
            AcceptedRankedScore acceptedRankedScore => acceptedRankedScore.RawPoints,
            _ => null as float?
        };

        if (pointGivingRawPoints is { } rawPoints)
            stringBuilder.Append('(')
                .Append(rawPoints.ToString("0.##")).Append(' ').Append(pointNamesById[rankedScore.PointId])
                .Append(") ");

        stringBuilder.AppendLine(score is Score.BeatLeaderScore { BeatLeaderScoreId: { } blScoreId }
            ? $" [Replay](https://replay.beatleader.com/?scoreId={blScoreId})"
            : null);
    }

    public static FlexHistory MakeNewFlexHistory(
        PlayerId playerId, GuildId guildId, ContextId contextId, DateTimeOffset dateTimeOffset,
        AchievementStatResponses.MemberAchievementStat[] achievementStats,
        ContextStatResponses.MemberContextStat contextStats) => new()
    {
        GuildId = guildId,
        PlayerId = playerId,
        ContextId = contextId,
        Timestamp = dateTimeOffset,
        GlobalAchievementId = achievementStats
            .Where(x => x.Achievement.CategoryId is null &&
                        x.Achievement.Progression is AchievementResponses.AchievementProgression.Ordered &&
                        !x.IsLocked)
            .LastOrDefault(x => x.IsCompleted)?.Achievement.Id is { } globalAchievementId
            ? new Achievement.AchievementId(globalAchievementId)
            : null,
        AchievementStats = achievementStats
            .Where(x => x.Achievement.CategoryId is not null &&
                        x.Achievement.Progression is AchievementResponses.AchievementProgression.Ordered &&
                        !x.IsLocked)
            .GroupBy(x => x.Achievement.CategoryId!.Value)
            .Select(x => new FlexHistoryAchievementStat
            {
                CategoryId = new CategoryId(x.Key),
                AchievementId = x.LastOrDefault(stat => stat.IsCompleted)?.Achievement.Id is { } achievementId
                    ? new Achievement.AchievementId(achievementId)
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
