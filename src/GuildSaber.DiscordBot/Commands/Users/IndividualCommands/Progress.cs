using System.Text;
using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Http;
using GuildSaber.Common.Helpers;
using GuildSaber.Common.Result;
using GuildSaber.DiscordBot.Core.AutocompleteHandlers;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Core.Handlers;
using GuildSaber.DiscordBot.Settings;
using static GuildSaber.Api.Features.Guilds.Levels.Http.LevelResponses;
using static GuildSaber.Api.Features.Guilds.Members.LevelStats.Http.LevelStatResponses;
using static GuildSaber.Api.Features.Guilds.Categories.Http.CategoryResponses;
using static GuildSaber.Api.Features.Players.Http.PlayerResponses;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [SlashCommand("progress", "Shows a specific player's progress, or depending on a specific category")]
    public async Task Progress(
        [Summary("Context")] [Autocomplete(typeof(ContextAutocompleteHandler))] ContextId contextId,
        [Summary("Category")] [Autocomplete(typeof(CategoryAutocompleteHandler))] CategoryId? categoryId = null,
        [Summary("User", "The user to show progress for (you if empty)")] IUser? user = null,
        [Summary("Visibility")] EDisplayChoice displayChoice = EDisplayChoice.Visible)
    {
        await DeferAsync(ephemeral: displayChoice.ToEphemeral());

        var (guild, player, category) = await (
                GetGuildAsync().AsTask(),
                user is null
                    ? GetPlayerAtMeAsync().AsTask()
                    : GetPlayerAsync(user.DiscordId).AsTask(),
                categoryId is not null
                    ? Cache.GetCategoryByIdAsync(categoryId.Value, Client.Value).AsTask()
                    : Task.FromResult<Category?>(null))
            .WhenAll();

        var stats = user is null
            ? (await Client.Value.LevelStats.GetAtMeAsync(contextId)).Unwrap()
            : (await Client.Value.LevelStats.GetByPlayerIdAsync(player.Id, contextId)).Unwrap();

        if (stats.Length == 0)
            throw user is null
                ? new InteractionHandler.CurrentPlayerDidNotJoinGuildContextException()
                : new InteractionHandler.PlayerIsNotInGuildContextException();

        var progressData = new ProgressCommand.ProgressData(
            Guild: guild,
            Player: player,
            Stats: stats,
            CategoryId: categoryId,
            PoolName: category?.Info.Name ?? "map",
            EmojiSettings.Value.Trophies
        );

        await FollowupAsync(components: ProgressCommand.MakeProgress(progressData));
    }
}

file static class ProgressCommand
{
    public readonly record struct ProgressData(
        GuildResponses.Guild Guild,
        Player Player,
        MemberLevelStat[] Stats,
        int? CategoryId,
        string PoolName,
        TrophyEmojis TrophyEmojis
    );

    public static MessageComponent MakeProgress(in ProgressData data)
    {
        var builder = new ComponentBuilderV2();
        var (categoryId, trophyEmojis) = (data.CategoryId, data.TrophyEmojis);
        var progressLines = data.Stats
            .Where(x => x.Level.CategoryId == categoryId)
            .Aggregate(new StringBuilder(), (sb, memberLevelStat) =>
            {
                var line = memberLevelStat.ToProgress(trophyEmojis);
                return line is null ? sb : sb.Append(memberLevelStat.Level.Info.Name).Append(' ').AppendLine(line);
            });

        var (userName, categoryName, color, avatarUrl) = (
            data.Player.PlayerInfo.Username,
            data.PoolName,
            Color.FromArgb(data.Guild.Info.Color),
            data.Player.PlayerInfo.AvatarUrl);

        builder.WithContainer(content => content
            .WithAccentColor(color)
            .WithSection(section => section
                .WithTextDisplay($"# {userName}'s progress\n" +
                                 $"Here is the current progress through the ***{categoryName}*** pools:")
                .WithAccessory(new ThumbnailBuilder()
                    .WithMedia(avatarUrl)))
            .WithTextDisplay(progressLines.ToString()switch
            {
                "" => ":x: There are currently no levels in this category.",
                var s => s
            }));

        return builder.Build();
    }

    private static string? ToProgress(this MemberLevelStat memberLevelStat, TrophyEmojis trophyEmojis)
        => memberLevelStat.Level switch
        {
            Level.RankedMapListLevel listLevel => GenerateProgressText(listLevel, memberLevelStat, trophyEmojis),
            _ => null
        };

    private static string GenerateProgressText(
        in Level.RankedMapListLevel level, in MemberLevelStat stat, TrophyEmojis trophyEmojis)
        => MakeProgressBar(stat.PassCount!.Value, level.TotalCount, 10) +
           trophyEmojis.GetFromPercentage(stat.PassCount!.Value / (double)level.TotalCount) switch
           {
               null => string.Empty,
               var (_, emoji) => $" {emoji}"
           } + $" ({stat.PassCount}/{level.TotalCount})";

    private static string MakeProgressBar(int value, int maxValue, int size)
    {
        if (size <= 0) return "[Invalid Progress Bar]";

        var percentage = maxValue == 0 ? 0 : Math.Clamp((float)value / maxValue, 0f, 1f);
        var progress = value > 0 ? Math.Max(1, (int)Math.Round(size * percentage)) : 0;

        var stringBuilder = new StringBuilder("[");
        for (var i = 0; i < size; i++)
            stringBuilder.Append(i < progress ? '▇' : '—');

        stringBuilder.Append(']');

        return stringBuilder.ToString();
    }
}