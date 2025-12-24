using System.Text;
using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Common.Result;
using GuildSaber.DiscordBot.AutocompleteHandlers;
using GuildSaber.DiscordBot.Core.Extensions;
using static GuildSaber.Api.Features.Guilds.Members.LevelStats.LevelStatResponses;
using static GuildSaber.Api.Features.Guilds.Categories.CategoryResponses;
using static GuildSaber.Api.Features.Players.PlayerResponses;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [SlashCommand("progress", "Shows a specific player's progress, or depending on a specific category")]
    public async Task Progress(
        [Summary("Context")] [Autocomplete(typeof(ContextAutocompleteHandler))] int contextId,
        [Summary("Category")] [Autocomplete(typeof(CategoryAutocompleteHandler))] int? categoryId = null,
        [Summary("VisibleToOther")] EDisplayChoice displayChoice = EDisplayChoice.Visible)
    {
        await DeferAsync(ephemeral: displayChoice.ToEphemeral());

        var guildTask = GetGuildAsync().AsTask();
        var playerTask = Client.Value.Players.GetAtMeAsync();
        var statsTask = Client.Value.LevelStats.GetAtMeAsync(contextId);
        var categoryNameTask = categoryId is not null
            ? Cache.GetCategoryByIdAsync(categoryId.Value, Client.Value).AsTask()
            : Task.FromResult<Category?>(null);

        await Task.WhenAll(guildTask, playerTask, statsTask, categoryNameTask);

        var progressData = new ProgressCommand.ProgressData(
            Guild: guildTask.Result,
            Player: playerTask.Result.Unwrap().ValueOrPlayerNotFoundException(),
            Stats: statsTask.Result.Unwrap(),
            CategoryId: categoryId,
            CategoryName: categoryId is not null ? categoryNameTask.Result!.Value.Info.Name : "map"
        );

        await FollowupAsync(embed: ProgressCommand.MakeProgress(progressData));
    }
}

file static class ProgressCommand
{
    public readonly record struct ProgressData(
        GuildResponses.Guild Guild,
        Player Player,
        MemberLevelStat[] Stats,
        int? CategoryId,
        string CategoryName
    );

    public static Embed MakeProgress(in ProgressData data)
    {
        var embedBuilder = new EmbedBuilder
        {
            Title = $"{data.Player.PlayerInfo.Username}'s Progress",
            ThumbnailUrl = data.Player.PlayerInfo.AvatarUrl,
            Color = Color.FromArgb(data.Guild.Info.Color),
            Description = $"Here is your current progress through the ***{data.CategoryName}*** pools:"
        };

        var categoryId = data.CategoryId;
        var progressLines = data.Stats
            .Where(x => x.Level.CategoryId == categoryId)
            .Select(memberLevelStat => $"{memberLevelStat.Level.Info.Name} {memberLevelStat.ToProgress()}")
            .ToList();

        var currentChunk = new StringBuilder();
        foreach (var lineWithNewline in progressLines.Select(line => line + "\n"))
        {
            if (currentChunk.Length + lineWithNewline.Length > EmbedBuilder.MaxFieldValueLength)
            {
                embedBuilder.AddField("\u200B", currentChunk.ToString());
                currentChunk.Clear();
            }

            currentChunk.Append(lineWithNewline);
        }

        if (currentChunk.Length > 0)
            embedBuilder.AddField("\u200B", currentChunk.ToString());

        return embedBuilder.Build();
    }

    private static string ToProgress(this in MemberLevelStat memberLevelStat)
        => memberLevelStat.Level switch
        {
            Level.RankedMapListLevel listLevel => GenerateProgressText(listLevel, memberLevelStat),
            _ => "[Currently unsupported Level Type]"
        };

    private static string GenerateProgressText(in Level.RankedMapListLevel level, in MemberLevelStat stat)
        => $"{MakeProgressBar(stat.PassCount!.Value, level.RankedMapCount, 10)} " +
           $"{Math.Round(stat.PassCount!.Value / (float)level.RankedMapCount * 100.0f)}% " +
           $"({stat.PassCount}/{level.RankedMapCount})";

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