using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.Api.Features.Players.Http;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Shared;
using GuildSaber.Common.Helpers;
using GuildSaber.CSharpClient;
using GuildSaber.DiscordBot.Core.AutocompleteHandlers;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Settings;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Filters = GuildSaber.Api.Features.RankedMaps.Http.RankedMapRequests.Filters;
using ERankedScoreType = GuildSaber.Api.Features.RankedScores.Http.RankedScoreRequests.ERankedScoreType;
using static GuildSaber.Api.Features.Guilds.Categories.Http.CategoryResponses;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [SlashCommand("ggp", "Displays ranked maps on a level alongside a player's ranked scores")]
    public async Task GetGrindPool(
        [Autocomplete<ContextAutocompleteHandler>] ContextId contextId,
        [Summary("Level")] int level,
        [Summary("Category", "The category to filter levels by"), Autocomplete<CategoryAutocompleteHandler>]
        int? categoryId = null,
        [Summary("Search", "The search term to find ranked maps")] string? search = null,
        [Summary("Need_Confirmation",
            "Whether to filter ranked maps by whether they require confirmation for ranked scores")]
        bool? needConfirmation = null,
        [Summary("User", "The user to show the ggp for (you if empty)")] IUser? user = null,
        [Summary("Visibility")] EDisplayChoice displayChoice = EDisplayChoice.Visible
    ) => await RespondAsync(ephemeral: displayChoice.ToEphemeral(), components: await SearchCommand
        .GetGgpComponentAsync(await GetGuildIdAsync(), contextId,
            await GetPlayerId(user?.DiscordId ?? Context.User.DiscordId),
            page: 1, level, Client.Value, Cache, EmojiSettings,
            new Filters(Search: search?.Trim(), DifficultyStarFrom: level, DifficultyStarTo: level,
                RankedScoreTypes: ERankedScoreType.PointGiving,
                CategoryIds: categoryId is null ? null : [categoryId.Value],
                NeedConfirmation: needConfirmation,
                MatchAnyCategory: true)));

    [ComponentInteraction("ggp_*_*_*_*_*_*_*_*_*")]
    public async Task Ggp(
        ContextId contextId, PlayerId playerId, CategoryId categoryId, int level, int page,
        ERankedScoreType rankedScoreTypes,
        int includeMapsWithoutScore,
        int needConfirmation,
        string search)
    {
        var component = await SearchCommand.GetGgpComponentAsync
        (await GetGuildIdAsync(), contextId, playerId, page: page, level, Client.Value, Cache, EmojiSettings,
            new Filters(Search: search.Trim(), DifficultyStarFrom: level, DifficultyStarTo: level,
                RankedScoreTypes: rankedScoreTypes,
                IncludeMapsWithoutScore: includeMapsWithoutScore == 1,
                CategoryIds: categoryId is { Value: 0 } ? null : [categoryId],
                NeedConfirmation: needConfirmation switch
                {
                    0 => false,
                    1 => true,
                    _ => null
                },
                MatchAnyCategory: true));

        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(msg => msg.Components = component);
    }

    [ComponentInteraction("ggp_*_*_*_*_*_*_*_*_")]
    public async Task Ggp(
        ContextId contextId, PlayerId playerId, CategoryId categoryId, int level, int page,
        ERankedScoreType rankedScoreTypes,
        int includeMapsWithoutScore,
        int needConfirmation)
    {
        var component = await SearchCommand.GetGgpComponentAsync
        (await GetGuildIdAsync(), contextId, playerId, page: page, level, Client.Value, Cache, EmojiSettings,
            new Filters(Search: null, DifficultyStarFrom: level, DifficultyStarTo: level,
                RankedScoreTypes: rankedScoreTypes,
                IncludeMapsWithoutScore: includeMapsWithoutScore == 1,
                CategoryIds: categoryId is { Value: 0 } ? null : [categoryId],
                MatchAnyCategory: true,
                NeedConfirmation: needConfirmation switch
                {
                    0 => false,
                    1 => true,
                    _ => null
                }
            ));

        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(msg => msg.Components = component);
    }
}

file static class SearchCommand
{
    public static async Task<MessageComponent> GetGgpComponentAsync(
        GuildId guildId, ContextId contextId, PlayerId playerId, int page, int levelOrder, GuildSaberClient client,
        HybridCache cache,
        IOptions<EmojiSettings> emojiSettings, Filters requestFilters)
    {
        var pageOption = new PaginatedRequestOptions<RankedMapRequests.ERankedMapSorter>
        {
            Page = Math.Max(page, 1),
            PageSize = 4,
            MaxPage = int.MaxValue,
            Order = EOrder.Desc,
            SortBy = RankedMapRequests.ERankedMapSorter.RankedScoreTime
        };

        var (categories, rankedMaps, playerResult) = await (
                cache.GetGuildCategoriesAsync(guildId, client).AsTask(),
                client.RankedMaps.GetWithScoresAsync(contextId, playerId, requestFilters, pageOption),
                client.Players.GetByIdAsync(playerId))
            .WhenAll();

        var player = playerResult
            .UnwrapOrCurrentPlayerDidNotJoinGuildContextException()
            .ValueOrCurrentPlayerNotRegisteredException();

        return !rankedMaps.TryGetValue(out var pagedRankedMaps, out var error)
            ? new ComponentBuilderV2().WithTextDisplay($"Error fetching ranked maps: {error}").Build()
            : BuildGgpComponent(pagedRankedMaps, player, categories, contextId, levelOrder, emojiSettings,
                requestFilters);
    }

    private static MessageComponent BuildGgpComponent(
        in PagedList<RankedMapResponses.RankedMapWithScores> pagedRankedMaps, PlayerResponses.Player player,
        Category[] categories, int contextId, int levelOrder, IOptions<EmojiSettings> emojiSettings,
        Filters requestFilters)
    {
        var builder = new ComponentBuilderV2();
        builder.WithContainer(content => content
            .WithSection(section => section
                .WithTextDisplay(
                    $"### [{player.PlayerInfo.Username}'s ggp](https://beatleader.com/u/{player.PlayerLinkedAccounts.BeatLeaderId})\n" +
                    $"For level **{levelOrder}**{(requestFilters.CategoryIds?.FirstOrDefault() is not (null or 0)
                        ? " in **" + categories.First(c => c.Id == requestFilters.CategoryIds[0]).Info.Name + "**"
                        : string.Empty)}" + requestFilters.NeedConfirmation switch
                    {
                        true => " *with* NeedConfirmation\n",
                        false => " *without* NeedConfirmation\n",
                        null => string.Empty
                    } + $"\n{(requestFilters.Search is null ? "" : $"> Search term: '{requestFilters.Search}'")}")
                .WithAccessory(new ThumbnailBuilder().WithMedia(player.PlayerInfo.AvatarUrl))));

        foreach (var data in pagedRankedMaps.Data)
            builder.WithContainer(data.RankedMap.ToContainerBuilder(data.RankedScores, categories, emojiSettings));

        builder.WithTextDisplay(pagedRankedMaps.TotalPages == 0
            ? "Nothing there!"
            : $"Page: **{pagedRankedMaps.Page}**/{pagedRankedMaps.TotalPages} " +
              $"({pagedRankedMaps.TotalCount} maps)"
        );

        var needConfirmationValue = requestFilters.NeedConfirmation switch
        {
            null => 2,
            true => 1,
            false => 0
        };

        var (page, hasPreviousPage, hasNextPage, playerId) =
            (pagedRankedMaps.Page, pagedRankedMaps.HasPreviousPage, pagedRankedMaps.HasNextPage, player.Id);
        var includeMapsWithoutScoreValue = requestFilters.IncludeMapsWithoutScore ? 1 : 0;
        var (prevCustomId, nextCustomId, unpassedCustomId, passedCustomId, pendingCustomId) = (
            $"ggp_{contextId}_{playerId}_{requestFilters.CategoryIds?.FirstOrDefault() ?? 0}_{levelOrder}_{page - 1}_{(int)requestFilters.RankedScoreTypes}_{includeMapsWithoutScoreValue}_{needConfirmationValue}_{requestFilters.Search}",
            $"ggp_{contextId}_{playerId}_{requestFilters.CategoryIds?.FirstOrDefault() ?? 0}_{levelOrder}_{page + 1}_{(int)requestFilters.RankedScoreTypes}_{includeMapsWithoutScoreValue}_{needConfirmationValue}_{requestFilters.Search}",
            $"ggp_{contextId}_{playerId}_{requestFilters.CategoryIds?.FirstOrDefault() ?? 0}_{levelOrder}_-2_{(int)ERankedScoreType.NonPointGivingNoPending}_1_{needConfirmationValue}_{requestFilters.Search}",
            $"ggp_{contextId}_{playerId}_{requestFilters.CategoryIds?.FirstOrDefault() ?? 0}_{levelOrder}_-3_{(int)ERankedScoreType.PointGiving}_0_{needConfirmationValue}_{requestFilters.Search}",
            $"ggp_{contextId}_{playerId}_{requestFilters.CategoryIds?.FirstOrDefault() ?? 0}_{levelOrder}_-4_{(int)ERankedScoreType.Pending}_0_{needConfirmationValue}_{requestFilters.Search}"
        );
        var searchTermTooLong = prevCustomId.Length > 100 || nextCustomId.Length > 100 ||
                                unpassedCustomId.Length > 100
                                || passedCustomId.Length > 100 || pendingCustomId.Length > 100;
        (prevCustomId, nextCustomId, unpassedCustomId, passedCustomId, pendingCustomId) = searchTermTooLong
            ? (prevCustomId[..100], nextCustomId[..100], unpassedCustomId[..100], passedCustomId[..100],
                pendingCustomId[..100])
            : (prevCustomId, nextCustomId, unpassedCustomId, passedCustomId, pendingCustomId);

        return builder.WithActionRow(new ActionRowBuilder()
                .WithButton(button => button
                    .WithLabel(!searchTermTooLong ? "Previous Page" : "Previous Page (search term too long!)")
                    .WithStyle(!searchTermTooLong ? ButtonStyle.Primary : ButtonStyle.Danger)
                    .WithDisabled(searchTermTooLong || !hasPreviousPage)
                    .WithCustomId(prevCustomId))
                .WithButton(button => button
                    .WithLabel(!searchTermTooLong ? "Next Page" : "Next Page (search term too long!)")
                    .WithStyle(!searchTermTooLong ? ButtonStyle.Primary : ButtonStyle.Danger)
                    .WithDisabled(searchTermTooLong || !hasNextPage)
                    .WithCustomId(nextCustomId))
                .WithButton(button => button
                    .WithLabel("Unpassed")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(IsUnpassed(requestFilters))
                    .WithCustomId(unpassedCustomId))
                .WithButton(button => button
                    .WithLabel("Passed")
                    .WithStyle(ButtonStyle.Success)
                    .WithDisabled(IsPassed(requestFilters))
                    .WithCustomId(passedCustomId))
                .WithButton(button => button
                    .WithLabel("Pending")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(IsPending(requestFilters))
                    .WithCustomId(pendingCustomId)))
            .Build();
    }

    private static bool IsPassed(Filters filters)
        => filters is { RankedScoreTypes: ERankedScoreType.PointGiving, IncludeMapsWithoutScore: false };

    private static bool IsUnpassed(Filters filters)
        => filters is { RankedScoreTypes: ERankedScoreType.NonPointGivingNoPending, IncludeMapsWithoutScore: true };

    private static bool IsPending(Filters filters)
        => filters is { RankedScoreTypes: ERankedScoreType.Pending, IncludeMapsWithoutScore: false };
}