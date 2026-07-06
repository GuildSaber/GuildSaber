using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Shared;
using GuildSaber.Common.Helpers;
using GuildSaber.CSharpClient;
using GuildSaber.DiscordBot.Core.AutocompleteHandlers;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Settings;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using static GuildSaber.Api.Features.Guilds.Categories.Http.CategoryResponses;

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
    public async Task Search(ContextId contextId, CategoryId categoryId, int page, int needConfirmation, string search)
    {
        var component = (await SearchCommand.GetRankedMapsComponentAsync
            (await GetGuildIdAsync(), contextId, new RankedMapRequests.Filters(Search: search,
                    CategoryIds: categoryId is { Value: 0 } ? null : [categoryId],
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

        var (categories, rankedMapsWithScores) = await (
                cache.GetGuildCategoriesAsync(guildId, client).AsTask(),
                client.RankedMaps.GetWithScoresAtMeAsync(contextId, requestFilters, pageOption))
            .WhenAll();

        return !rankedMapsWithScores.TryGetValue(out var pagedRankedMapsWithScores, out var error)
            ? new ComponentBuilderV2().WithTextDisplay($"Error fetching ranked maps: {error}")
            : BuildSearchComponent(pagedRankedMapsWithScores, categories, contextId, requestFilters, emojiSettings);
    }

    private static ComponentBuilderV2 BuildSearchComponent(
        in PagedList<RankedMapResponses.RankedMapWithScores> pagedRankedMapsWithScores,
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

        if (pagedRankedMapsWithScores.Data.Length == 0)
            return builder.WithTextDisplay(pagedRankedMapsWithScores.Page != 1
                ? $"(Page {pagedRankedMapsWithScores.Page}), No ranked maps{needConfirmationText} found for the search term: {requestFilters.Search}{
                    (requestFilters.CategoryIds?.FirstOrDefault() is not (null or 0)
                        ? " in **" + categories.First(c => c.Id == requestFilters.CategoryIds[0]).Info.Name + "**"
                        : string.Empty)}.\n" + "You might want to go back to page 1."
                : $"No ranked maps{needConfirmationText} found for the search term: {requestFilters.Search}.\n***Tips:*** " +
                  "You can also write the __bsr key__, the __mapper name__, the __map hash__, and so on..");

        foreach (var data in pagedRankedMapsWithScores.Data)
            builder.WithContainer(data.RankedMap.ToContainerBuilder(data.RankedScores, categories, emojiSettings));

        if (pagedRankedMapsWithScores.TotalCount == pagedRankedMapsWithScores.Data.Length)
            return builder.WithTextDisplay(
                $"Found **{pagedRankedMapsWithScores.TotalCount}** ranked maps{needConfirmationText}" +
                $" for the search term: '{requestFilters.Search}'{
                    (requestFilters.CategoryIds?.FirstOrDefault() is not (null or 0)
                        ? " in **" + categories.First(c => c.Id == requestFilters.CategoryIds[0]).Info.Name + "**"
                        : string.Empty)}.");

        builder.WithTextDisplay(
            $"(Page: **{pagedRankedMapsWithScores.Page}**/{pagedRankedMapsWithScores.TotalPages}) " +
            $"Found **{pagedRankedMapsWithScores.TotalCount}** ranked maps{needConfirmationText} for the search term: '{requestFilters.Search}'{
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
                $"search_{contextId}_{requestFilters.CategoryIds?.FirstOrDefault() ?? 0}_{pagedRankedMapsWithScores.Page - 1}_{needConfirmationValue}_{requestFilters.Search}"
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
                            .WithDisabled(!pagedRankedMapsWithScores.HasPreviousPage)
                    })
            .WithButton(
                $"search_{contextId}_{requestFilters.CategoryIds?.FirstOrDefault() ?? 0}_{pagedRankedMapsWithScores.Page + 1}_{needConfirmationValue}_{requestFilters.Search}"
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
                            .WithDisabled(!pagedRankedMapsWithScores.HasNextPage)
                    }));

        return builder;
    }
}