using System.Text;
using Discord;
using Discord.Interactions;
using GuildSaber.Common.Result;
using GuildSaber.CSharpClient;
using GuildSaber.Database.Models.StrongTypes;
using GuildSaber.DiscordBot.AutocompleteHandlers;
using GuildSaber.DiscordBot.Core.Extensions;
using Microsoft.Extensions.Caching.Hybrid;
using static GuildSaber.Api.Features.Guilds.Members.LevelStats.LevelStatResponses;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [SlashCommand("progress", "Shows a specific player's progress, or depending on a specific category")]
    public async Task Me(
        [Summary("Context")] [Autocomplete(typeof(ContextAutocompleteHandler))] int contextId,
        [Summary("Category")] [Autocomplete(typeof(CategoryAutocompleteHandler))] int? categoryId = null,
        [Summary("VisibleToOther")] EDisplayChoice displayChoice = EDisplayChoice.Visible)
    {
        await DeferAsync(ephemeral: displayChoice.ToEphemeral());
        await FollowupAsync(embed: await ProgramCommand.MakeProgress(Client.Value, Cache, Context.Guild.DiscordId,
            contextId, categoryId));
    }
}

file static class ProgramCommand
{
    public static async Task<Embed> MakeProgress(
        GuildSaberClient client, HybridCache cache, DiscordGuildId discordGuildId, int contextId, int? categoryId)
    {
        var (guild, player, stats) = (
            (await cache.GetGuildFromDiscordGuildIdAsync(discordGuildId, client)).ValueOrGuildMissingException(),
            (await client.Players.GetAtMeAsync(CancellationToken.None)).Unwrap().ValueOrPlayerNotFoundException(),
            (await client.LevelStats.GetAtMeAsync(contextId, CancellationToken.None)).Unwrap()
        );

        var embedBuilder = new EmbedBuilder
        {
            Title = $"{player.PlayerInfo.Username}'s Progress",
            ThumbnailUrl = player.PlayerInfo.AvatarUrl,
            Color = Color.FromArgb(guild.Info.Color),
            Description = $"Here is your current progress through the ***{(categoryId is not null
                ? (await cache.GetCategoryByIdAsync(categoryId.Value, client))!.Value.Info.Name
                : "map")}*** pools:"
        };

        var progressLines = stats
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

    private static string ToProgress(this MemberLevelStat memberLevelStat)
        => memberLevelStat.Level switch
        {
            Level.RankedMapListLevel listLevel => GenerateProgressText(listLevel, memberLevelStat),
            _ => "[Currently unsupported Level Type]"
        };

    private static string GenerateProgressText(Level.RankedMapListLevel level, MemberLevelStat stat)
        => $"{MakeProgressBar(stat.PassCount!.Value, level.RankedMapCount, 10)} " +
           $"{Math.Round(stat.PassCount!.Value / (float)level.RankedMapCount * 100.0f)}% " +
           $"({stat.PassCount}/{level.RankedMapCount})";

    private static string MakeProgressBar(int value, int maxValue, int size)
    {
        if (maxValue <= 0 || size <= 0)
            return "[Invalid Progress Bar]";

        var percentage = Math.Clamp((float)value / maxValue, 0f, 1f);
        var progress = value > 0 ? Math.Max(1, (int)Math.Round(size * percentage)) : 0;

        var stringBuilder = new StringBuilder("[");
        for (var i = 0; i < size; i++)
            stringBuilder.Append(i < progress ? '▇' : '—');

        stringBuilder.Append(']');

        return stringBuilder.ToString();
    }
}