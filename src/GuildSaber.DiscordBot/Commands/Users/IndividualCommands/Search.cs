using System.Text;
using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.CSharpClient;
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
        [Autocomplete<ContextAutocompleteHandler>] ContextId contextId,
        [Summary("Search", "The search term to find ranked maps")] string search,
        [Summary("Category", "The category to filter by"), Autocomplete<CategoryAutocompleteHandler>] int? categoryId =
            null,
        [Summary("Need_Confirmation",
            "Whether to filter ranked maps by whether they require confirmation for ranked scores")]
        bool? needConfirmation = null,
        [Summary("Page", "Page number for pagination")] int page = 1,
        [Summary("Visibility")] EDisplayChoice displayChoice = EDisplayChoice.Visible
    ) => await RespondAsync(ephemeral: displayChoice.ToEphemeral(), components: (await SearchCommand
            .GetRankedMapsComponentAsync(
                await GetGuildIdAsync(), contextId, new RankedMapRequests.Filters(Search: search.Trim(),
                    CategoryIds: categoryId is null ? null : [categoryId.Value],
                    MatchAnyCategory: true, NeedConfirmation: needConfirmation),
                page, Client.Value, Cache, EmojiSettings))
        .Build());

    [ComponentInteraction("search_*_*_*_*_*")]
    public async Task Search(ContextId contextId, int categoryId, int page, int needConfirmation, string search)
    {
        var component = (await SearchCommand.GetRankedMapsComponentAsync
            (await GetGuildIdAsync(), contextId, new RankedMapRequests.Filters(Search: search,
                    CategoryIds: categoryId is 0 ? null : [categoryId],
                    MatchAnyCategory: true,
                    NeedConfirmation: needConfirmation switch
                    {
                        0 => false,
                        1 => true,
                        _ => null
                    }),
                page, Client.Value, Cache, EmojiSettings))
            .Build();

        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(msg => msg.Components = component);
    }
}

file static class SearchCommand
{
    public static async Task<ComponentBuilderV2> GetRankedMapsComponentAsync(
        GuildId guildId, ContextId contextId, RankedMapRequests.Filters requestFilters,
        int page, GuildSaberClient client, HybridCache cache,
        IOptions<EmojiSettings> emojiSettings)
    {
        var pageOption = new PaginatedRequestOptions<RankedMapRequests.ERankedMapSorter>
        {
            Page = Math.Max(1, page),
            PageSize = 4,
            MaxPage = int.MaxValue,
            Order = EOrder.Desc,
            SortBy = RankedMapRequests.ERankedMapSorter.Name
        };

        var categoriesTask = cache.GetGuildCategoriesAsync(guildId, client).AsTask();
        var rankedMapsTask = client.RankedMaps.GetAsync(contextId, requestFilters, pageOption);

        await Task.WhenAll(categoriesTask, rankedMapsTask);
        var (categories, rankedMaps) = (categoriesTask.Result, rankedMapsTask.Result);

        return !rankedMaps.TryGetValue(out var pagedRankedMaps, out var error)
            ? new ComponentBuilderV2().WithTextDisplay($"Error fetching ranked maps: {error}")
            : BuildSearchComponent(pagedRankedMaps, categories, contextId, requestFilters, emojiSettings);
    }

    private static ComponentBuilderV2 BuildSearchComponent(
        in PagedList<RankedMapResponses.RankedMap> pagedRankedMaps,
        Category[] categories,
        int contextId,
        RankedMapRequests.Filters requestFilters,
        IOptions<EmojiSettings> emojiSettings)
    {
        var builder = new ComponentBuilderV2();
        var needConfirmationText = requestFilters.NeedConfirmation switch
        {
            true => " that require confirmation",
            false => " that don't require confirmation",
            null => string.Empty
        };

        if (pagedRankedMaps.Data.Length == 0)
            return builder.WithTextDisplay(pagedRankedMaps.Page != 1
                ? $"(Page {pagedRankedMaps.Page}), No ranked maps{needConfirmationText} found for the search term: {requestFilters.Search}{
                    (requestFilters.CategoryIds?.FirstOrDefault() is not (null or 0)
                        ? " in **" + categories.First(c => c.Id == requestFilters.CategoryIds[0]).Info.Name + "**"
                        : string.Empty)}.\n" + "You might want to go back to page 1."
                : $"No ranked maps{needConfirmationText} found for the search term: {requestFilters.Search}.\n***Tips:*** " +
                  "You can also write the __bsr key__, the __mapper name__, the __map hash__, and so on..");

        foreach (var rankedMap in pagedRankedMaps.Data)
            builder.WithContainer(BuildRankedMapDisplayContainer(rankedMap, categories, emojiSettings));

        if (pagedRankedMaps.TotalCount == pagedRankedMaps.Data.Length)
            return builder.WithTextDisplay($"Found **{pagedRankedMaps.TotalCount}** ranked maps{needConfirmationText}" +
                                           $" for the search term: '{requestFilters.Search}'{
                                               (requestFilters.CategoryIds?.FirstOrDefault() is not (null or 0)
                                                   ? " in **" + categories.First(c => c.Id == requestFilters.CategoryIds[0]).Info.Name + "**"
                                                   : string.Empty)}.");

        builder.WithTextDisplay($"(Page: **{pagedRankedMaps.Page}**/{pagedRankedMaps.TotalPages}) " +
                                $"Found **{pagedRankedMaps.TotalCount}** ranked maps{needConfirmationText} for the search term: '{requestFilters.Search}'{
                                    (requestFilters.CategoryIds?.FirstOrDefault() is not (null or 0)
                                        ? " in **" + categories.First(c => c.Id == requestFilters.CategoryIds[0]).Info.Name + "**"
                                        : string.Empty)}.");

        var needConfirmationValue = requestFilters.NeedConfirmation switch
        {
            true => 1,
            false => 0,
            null => 2
        };

        builder.WithActionRow(new ActionRowBuilder()
            .WithButton(
                $"search_{contextId}_{requestFilters.CategoryIds?.FirstOrDefault() ?? 0}_{pagedRankedMaps.Page - 1}_{needConfirmationValue}_{requestFilters.Search}"
                    switch
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
            .WithButton(
                $"search_{contextId}_{requestFilters.CategoryIds?.FirstOrDefault() ?? 0}_{pagedRankedMaps.Page + 1}_{needConfirmationValue}_{requestFilters.Search}"
                    switch
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
            if (i > 0) mapContainerBuilder.WithSeparator();
            else mapContainerBuilder.WithAccentColor(Color.FromDifficulty(version.Difficulty.Difficulty));

            var sb = new StringBuilder()
                .Append("**[").Append(song.Info.BeatSaverName).Append("](https://beatsaver.com/maps/")
                .Append(song.Key).Append(")**")
                .Append(" (").Append(song.Key is { } key ? key.ToBsrKey() : "no !bsr").Append(")\n")
                .Append("Mapper(s): ").AppendLine(song.Info.MapperName)
                .Append("Difficulty: ").AppendLine(version.Difficulty.Difficulty.ToString())
                .AppendLine();

            sb.Append("⭐: ").Append(rankedMap.Rating.DiffStar.ToString("0.00")).Append(" | ");
            sb.Append("✨: ").Append(rankedMap.Rating.AccStar.ToString("0.00"));

            if (rankedMap.Requirements.MinAccuracy is { } minAcc)
                sb.Append(" (Acc > ").Append(minAcc.ToString("0.##")).Append("%)");

            sb.AppendLine();

            if (rankedMap.CategoryIds.Length != 0)
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
                .Append(TimeSpan.FromSeconds(version.Song.Stats.DurationSec) switch
                {
                    { Hours: > 0 } ts => ts.ToString(@"hh\:mm\:ss"),
                    var ts => ts.ToString(@"mm\:ss")
                }).Append(" | ")
                .Append("BPM: ").Append(version.Song.Stats.BPM.ToString("0.##"))
                .AppendLine();

            if (rankedMap.Requirements.ProhibitedModifiers != RankedMapRequests.EModifiers.ProhibitedDefaults)
                sb.AppendLine()
                    .Append("Prohibited Modifiers: ")
                    .Append(rankedMap.Requirements.ProhibitedModifiers);

            if (rankedMap.Requirements.MandatoryModifiers != RankedMapRequests.EModifiers.None)
                sb.AppendLine()
                    .Append("Mandatory Modifiers: ")
                    .Append(rankedMap.Requirements.MandatoryModifiers | RankedMapRequests.EModifiers.FasterSong);

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

            if (i > 0)
                mapContainerBuilder.WithTextDisplay(sb.ToString());
            else
                mapContainerBuilder.WithSection(sectionBuilder
                    .WithTextDisplay(sb.ToString())
                    .WithAccessory(new ThumbnailBuilder()
                        .WithMedia($"https://cdn.beatsaver.com/{rankedMap.Versions[0].Song.Hash}.jpg")));
        }

        return mapContainerBuilder;
    }
}