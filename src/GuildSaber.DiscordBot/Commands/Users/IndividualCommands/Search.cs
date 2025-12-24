using System.Text;
using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.CSharpClient;
using GuildSaber.CSharpClient.Routes.Internal;
using GuildSaber.DiscordBot.AutocompleteHandlers;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Settings;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using static GuildSaber.Api.Features.Guilds.Categories.CategoryResponses;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [SlashCommand("search", "Search ranked maps on the context by a query")]
    public async Task Search(
        [Autocomplete<ContextAutocompleteHandler>] int contextId,
        [Summary("search", "The search term to find ranked maps")] string search,
        [Summary("page", "Page number for pagination")] int page = 1,
        [Summary("VisibleToOther")] EDisplayChoice displayChoice = EDisplayChoice.Invisible
    ) => await RespondAsync(ephemeral: displayChoice.ToEphemeral(), components: (await SearchCommand
            .GetRankedMapsComponentAsync(await GetGuildIdAsync(), contextId, search, page, Client.Value, Cache,
                EmojiSettings))
        .Build());

    [ComponentInteraction("search_prev_*_*_*")]
    public async Task SearchPreviousPage(int contextId, string search, int page)
    {
        var component = (await SearchCommand.GetRankedMapsComponentAsync
                (await GetGuildIdAsync(), contextId, search, page, Client.Value, Cache, EmojiSettings))
            .Build();

        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(msg => msg.Components = component);
    }

    [ComponentInteraction("search_next_*_*_*")]
    public async Task SearchNextPage(int contextId, string search, int page)
    {
        var component = (await SearchCommand.GetRankedMapsComponentAsync
                (await GetGuildIdAsync(), contextId, search, page, Client.Value, Cache, EmojiSettings))
            .Build();

        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(msg => msg.Components = component);
    }
}

file static class SearchCommand
{
    public static async Task<ComponentBuilderV2> GetRankedMapsComponentAsync(
        GuildId guildId, int contextId, string search, int page, GuildSaberClient client, HybridCache cache,
        IOptions<EmojiSettings> emojiSettings)
    {
        var pageOption = new PaginatedRequestOptions<RankedMapRequest.ERankedMapSorter>
        {
            Page = page,
            PageSize = 3,
            MaxPage = 3,
            Order = EOrder.Desc,
            SortBy = RankedMapRequest.ERankedMapSorter.Name
        };

        var categoriesTask = cache.GetGuildCategoriesAsync(guildId, client).AsTask();
        var rankedMapsTask = client.RankedMaps.GetAsync(contextId, search, pageOption);

        await Task.WhenAll(categoriesTask, rankedMapsTask);

        var categories = categoriesTask.Result;
        var rankedMaps = rankedMapsTask.Result;

        return !rankedMaps.TryGetValue(out var pagedRankedMaps, out var error)
            ? new ComponentBuilderV2().WithTextDisplay($"Error fetching ranked maps: {error}")
            : BuildSearchComponent(pagedRankedMaps, categories, contextId, search, page, emojiSettings);
    }

    private static ComponentBuilderV2 BuildSearchComponent(
        in PagedList<RankedMapResponses.RankedMap> pagedRankedMaps,
        Category[] categories,
        int contextId,
        string search,
        int page,
        IOptions<EmojiSettings> emojiSettings)
    {
        var builder = new ComponentBuilderV2();

        if (pagedRankedMaps.Data.Length == 0)
            return builder.WithTextDisplay(pagedRankedMaps.Page != 1
                ? $"(Page {pagedRankedMaps.Page}), No ranked maps found for the search term: {search}.\n" +
                  "You might want to go back to page 1."
                : $"No ranked maps found for the search term: {search}.\n***Tips:*** " +
                  "You can also write the __bsr key__, the __mapper name__, the __map hash__, and so on..");

        foreach (var rankedMap in pagedRankedMaps.Data)
            builder.WithContainer(BuildRankedMapDisplayContainer(rankedMap, categories, emojiSettings));

        if (pagedRankedMaps.TotalCount == pagedRankedMaps.Data.Length)
            return builder.WithTextDisplay(
                $"Found **{pagedRankedMaps.TotalCount}** ranked maps for the search term: '{search}'.");

        builder.WithTextDisplay($"(Page: **{pagedRankedMaps.Page}**/{pagedRankedMaps.TotalPages}) " +
                                $"Found **{pagedRankedMaps.TotalCount}** ranked maps for the search term: '{search}'.");

        builder.WithActionRow(new ActionRowBuilder()
            .WithButton($"search_prev_{contextId}_{search}_{page - 1}" switch
            {
                { Length: > 100 } => new ButtonBuilder()
                    .WithLabel("Previous Page (search term too long!)")
                    .WithStyle(ButtonStyle.Danger)
                    .WithDisabled(true),
                var id => new ButtonBuilder()
                    .WithLabel("Previous Page")
                    .WithStyle(ButtonStyle.Primary)
                    .WithCustomId(id)
                    .WithDisabled(pagedRankedMaps.Page <= 1)
            })
            .WithButton($"search_next_{contextId}_{search}_{page + 1}" switch
            {
                { Length: > 100 } => new ButtonBuilder()
                    .WithLabel("Next Page (search term too long!)")
                    .WithStyle(ButtonStyle.Danger)
                    .WithDisabled(true),
                var id => new ButtonBuilder()
                    .WithLabel("Next Page")
                    .WithStyle(ButtonStyle.Primary)
                    .WithCustomId(id)
                    .WithDisabled(pagedRankedMaps.Page >= pagedRankedMaps.TotalPages)
            }));

        return builder;
    }

    private static ContainerBuilder BuildRankedMapDisplayContainer(
        RankedMapResponses.RankedMap rankedMap,
        Category[] categories,
        IOptions<EmojiSettings> emojiSettings)
    {
        var sectionBuilder = new SectionBuilder();
        var mapContainerBuilder = new ContainerBuilder();

        foreach (var (i, version) in rankedMap.Versions.Index())
        {
            var song = version.Song;
            if (i > 0) sectionBuilder.WithSeparator();

            var sb = new StringBuilder()
                .Append("**[").Append(song.Info.BeatSaverName).Append("](https://beatsaver.com/maps/")
                .Append(song.Key).Append(")**")
                .Append(" (key-").Append(song.Key).Append(")\n")
                .Append("Mapper(s): ").AppendLine(song.Info.MapperName)
                .Append("Difficulty: ").AppendLine(version.Difficulty.Difficulty.ToString())
                .AppendLine();

            sb.Append("⭐: ").Append(rankedMap.Rating.DiffStar.ToString("0.00")).Append(" | ");
            sb.Append("✨: ").Append(rankedMap.Rating.AccStar.ToString("0.00"));

            if (rankedMap.Requirements.MinAccuracy is { } minAcc)
                sb.Append(" (Acc < ").Append(minAcc.ToString("0.##")).Append("%)");

            sb.AppendLine();

            if (rankedMap.CategoryIds?.Any() == true)
            {
                sb.Append("Categories: ");
                var categoryNames = rankedMap.CategoryIds
                    .Select(id => categories.TryFirst(c => c.Id == id)
                        .Match(x => x.Info.Name, () => "Unknown"))
                    .ToArray();
                sb.Append(string.Join(", ", categoryNames));
            }

            sb.AppendLine()
                .Append("NJS: ").Append(version.Difficulty.Stats.NJS.ToString("0.##")).Append(" | ")
                .Append("Length: ")
                .Append(TimeSpan.FromSeconds(version.Difficulty.Stats.Duration) switch
                {
                    { Hours: > 0 } ts => ts.ToString(@"hh\:mm\:ss"),
                    var ts => ts.ToString(@"mm\:ss")
                }).Append(" | ")
                .Append("BPM: ").Append(version.Song.Stats.BPM.ToString("0.##"))
                .AppendLine();

            if (rankedMap.Requirements.ProhibitedModifiers != RankedMapRequest.EModifiers.ProhibitedDefaults)
                sb.AppendLine()
                    .Append("Prohibited Modifiers: ")
                    .Append(rankedMap.Requirements.ProhibitedModifiers);

            if (rankedMap.Requirements.MandatoryModifiers != RankedMapRequest.EModifiers.None)
                sb.AppendLine()
                    .Append("Mandatory Modifiers: ")
                    .Append(rankedMap.Requirements.MandatoryModifiers | RankedMapRequest.EModifiers.FasterSong);

            if (rankedMap.Requirements.NeedConfirmation
                || rankedMap.Requirements.MaxPauseDurationSec is not null
                || rankedMap.Requirements.NeedFullCombo)
            {
                sb.AppendLine()
                    .Append("Requirements: ");

                if (rankedMap.Requirements.NeedFullCombo)
                    sb.Append("***FC***, ");

                if (rankedMap.Requirements.MaxPauseDurationSec is { } pauseSecs)
                    sb.Append("⏸️ < ").Append(pauseSecs.ToString("0.##")).Append("s, ");

                if (rankedMap.Requirements.NeedConfirmation)
                    sb.Append(emojiSettings.Value.NeedConfirmation).Append(", ");

                // Remove last ", "
                sb.Length -= 2;
            }

            sectionBuilder.WithTextDisplay(sb.ToString());
            mapContainerBuilder.WithAccentColor(Color.FromDifficulty(version.Difficulty.Difficulty));
        }

        mapContainerBuilder.WithSection(sectionBuilder.WithAccessory(new ThumbnailBuilder().WithMedia(
            $"https://cdn.beatsaver.com/{rankedMap.Versions[0].Song.Hash}.jpg")));

        return mapContainerBuilder;
    }
}