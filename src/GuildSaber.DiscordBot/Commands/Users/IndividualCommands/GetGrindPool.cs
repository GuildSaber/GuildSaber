using System.Text;
using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Api.Features.Players;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Api.Features.RankedScores;
using GuildSaber.Common.Helpers;
using GuildSaber.CSharpClient;
using GuildSaber.Database.Models.StrongTypes;
using GuildSaber.DiscordBot.AutocompleteHandlers;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Settings;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Filters = GuildSaber.Api.Features.RankedMaps.RankedMapRequests.Filters;
using EState = GuildSaber.Api.Features.RankedScores.RankedScoreResponses.EState;
using static GuildSaber.Api.Features.Guilds.Categories.CategoryResponses;

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
                AnyRankedScoreStates: EState.Selected,
                ExcludeRankedScoreStates: EState.NonPointGiving,
                CategoryIds: categoryId is null ? null : [categoryId.Value],
                NeedConfirmation: needConfirmation,
                MatchAnyCategory: true)));

    [ComponentInteraction("ggp_*_*_*_*_*_*_*_*_*_*")]
    public async Task Ggp(
        ContextId contextId, PlayerId playerId, int categoryId, int level, int page,
        EState anyRankedScoreStates,
        EState allRankedScoreStates,
        EState excludeRankedScoreStates,
        int needConfirmation,
        string search)
    {
        var component = await SearchCommand.GetGgpComponentAsync
        (await GetGuildIdAsync(), contextId, playerId, page: page, level, Client.Value, Cache, EmojiSettings,
            new Filters(Search: search.Trim(), DifficultyStarFrom: level, DifficultyStarTo: level,
                AnyRankedScoreStates: anyRankedScoreStates,
                AllRankedScoreStates: allRankedScoreStates,
                ExcludeRankedScoreStates: excludeRankedScoreStates,
                CategoryIds: categoryId is 0 ? null : [categoryId],
                NeedConfirmation: needConfirmation switch
                {
                    0 => false,
                    1 => true,
                    _ => null
                },
                MatchAnyCategory: true));

        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(msg => msg.Components = component);
    }

    [ComponentInteraction("ggp_*_*_*_*_*_*_*_*_*_")]
    public async Task Ggp(
        ContextId contextId, PlayerId playerId, int categoryId, int level, int page,
        EState anyRankedScoreStates,
        EState allRankedScoreStates,
        EState excludeRankedScoreStates,
        int needConfirmation)
    {
        var component = await SearchCommand.GetGgpComponentAsync
        (await GetGuildIdAsync(), contextId, playerId, page: page, level, Client.Value, Cache, EmojiSettings,
            new Filters(Search: null, DifficultyStarFrom: level, DifficultyStarTo: level,
                AnyRankedScoreStates: anyRankedScoreStates,
                AllRankedScoreStates: allRankedScoreStates,
                ExcludeRankedScoreStates: excludeRankedScoreStates,
                CategoryIds: categoryId is 0 ? null : [categoryId],
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
        if (requestFilters.AnyRankedScoreStates == EState.None)
            requestFilters.AnyRankedScoreStates = null;

        if (requestFilters.AllRankedScoreStates == EState.None)
            requestFilters.AllRankedScoreStates = null;

        if (requestFilters.ExcludeRankedScoreStates == EState.None)
            requestFilters.ExcludeRankedScoreStates = null;

        var pageOption = new PaginatedRequestOptions<RankedMapRequests.ERankedMapSorter>
        {
            Page = Math.Max(page, 1),
            PageSize = 4,
            MaxPage = int.MaxValue,
            Order = EOrder.Desc,
            SortBy = RankedMapRequests.ERankedMapSorter.RankedScoreTime
        };

        var categoriesTask = cache.GetGuildCategoriesAsync(guildId, client).AsTask();
        var rankedMapsTask = client.RankedMaps.GetWithScoreAsync(contextId, playerId, requestFilters, pageOption);
        var playerTask = client.Players.GetByIdAsync(playerId);

        await Task.WhenAll(categoriesTask, rankedMapsTask, playerTask);
        var (categories, rankedMaps, player) = (categoriesTask.Result, rankedMapsTask.Result, playerTask.Result
            .UnwrapOrCurrentPlayerDidNotJoinGuildContextException()
            .ValueOrCurrentPlayerNotRegisteredException());

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

        foreach (var rankedMap in pagedRankedMaps.Data)
            builder.WithContainer(BuildRankedMapWithScoresDisplayContainer(rankedMap, categories, emojiSettings));

        builder.WithTextDisplay(
            pagedRankedMaps.TotalPages == 0
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

        var (page, totalPages, playerId) = (pagedRankedMaps.Page, pagedRankedMaps.TotalPages, player.Id);
        var (prevCustomId, nextCustomId, unpassedCustomId, passedCustomId, pendingCustomId) = (
            $"ggp_{contextId}_{playerId}_{requestFilters.CategoryIds?.FirstOrDefault() ?? 0}_{levelOrder}_{page - 1}_{(int)(requestFilters.AnyRankedScoreStates ?? EState.None)}_{(int)EState.None}_" +
            $"{(int)(requestFilters.ExcludeRankedScoreStates ?? EState.None)}_{needConfirmationValue}_{requestFilters.Search}",
            $"ggp_{contextId}_{playerId}_{requestFilters.CategoryIds?.FirstOrDefault() ?? 0}_{levelOrder}_{page + 1}_{(int)(requestFilters.AnyRankedScoreStates ?? EState.None)}_{(int)EState.None}_" +
            $"{(int)(requestFilters.ExcludeRankedScoreStates ?? EState.None)}_{needConfirmationValue}_{requestFilters.Search}",
            $"ggp_{contextId}_{playerId}_{requestFilters.CategoryIds?.FirstOrDefault() ?? 0}_{levelOrder}_-2_{(int)EState.NonPointGivingNoPending}_{(int)EState.None}_" +
            $"{(int)EState.Pending}_{needConfirmationValue}_{requestFilters.Search}",
            $"ggp_{contextId}_{playerId}_{requestFilters.CategoryIds?.FirstOrDefault() ?? 0}_{levelOrder}_-3_{(int)EState.Selected}_{(int)EState.None}_" +
            $"{(int)EState.NonPointGiving}_{needConfirmationValue}_{requestFilters.Search}",
            $"ggp_{contextId}_{playerId}_{requestFilters.CategoryIds?.FirstOrDefault() ?? 0}_{levelOrder}_-4_{(int)EState.None}_{(int)(EState.Selected | EState.Pending)}_" +
            $"{(int)EState.NonPointGivingNoPending}_{(int)EState.NonPointGivingNoPending}_{needConfirmationValue}_{requestFilters.Search}"
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
                    .WithDisabled(searchTermTooLong || page <= 1)
                    .WithCustomId(prevCustomId))
                .WithButton(button => button
                    .WithLabel(!searchTermTooLong ? "Next Page" : "Next Page (search term too long!)")
                    .WithStyle(!searchTermTooLong ? ButtonStyle.Primary : ButtonStyle.Danger)
                    .WithDisabled(searchTermTooLong || page >= totalPages)
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

    private static bool IsPassed(Filters filters) => filters.AnyRankedScoreStates == EState.Selected;

    private static bool IsUnpassed(Filters filters)
        => filters.AnyRankedScoreStates == EState.NonPointGivingNoPending;

    private static bool IsPending(Filters filters)
        => filters.AllRankedScoreStates == (EState.Selected | EState.Pending);

    private static ContainerBuilder BuildRankedMapWithScoresDisplayContainer(
        RankedMapResponses.RankedMapWithScores data,
        Category[] categories,
        IOptions<EmojiSettings> emojiSettings)
    {
        var sectionBuilder = new SectionBuilder();
        var mapContainerBuilder = new ContainerBuilder();

        var rankedMap = data.RankedMap;
        var rankedScores = data.RankedScores;
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
                .Append("Difficulty: ").Append(version.Difficulty.Difficulty.ToString())
                .Append(", ").AppendLine(version.Difficulty.GameMode)
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

            // There should always only be one score, so you shouldn't really need to worry about anything there.
            foreach (var rankedScore in rankedScores)
            {
                mapContainerBuilder.WithSeparator();

                var score = rankedScore.Score;

                sb.Clear()
                    .Append(rankedScore.State switch
                    {
                        _ when rankedScore.State.HasAnyFlag(EState.NonPointGivingNoPending) => ":x: ",
                        _ when rankedScore.State.HasAnyFlag(EState.Pending) => ":hourglass: ",
                        _ when rankedScore.State.HasAnyFlag(EState.Selected) => ":white_check_mark: ",
                        _ => string.Empty
                    }).Append(((float)Accuracy.From(
                            BaseScore.CreateUnsafe(score.BaseScore).Value,
                            MaxScore.CreateUnsafe(version.Difficulty.Stats.MaxScore).Value))
                        .ToString("N1")).Append("% ");

                if (rankedScore.PrevScore is { } prevScore)
                {
                    var diff = score.BaseScore - prevScore.BaseScore;
                    sb.Append(" (").Append(diff >= 0 ? "+" : "").Append(diff.ToString("N0")).Append(") ");
                }

                if (rankedScore.Score.Modifiers != RankedMapRequests.EModifiers.None)
                    sb.Append(" | Mods: ").Append(rankedScore.Score.Modifiers).Append(' ');

                sb.Append(TimestampTag.FormatFromDateTimeOffset(score.SetAt, TimestampTagStyles.ShortDateTime))
                    .AppendLine(score is RankedScoreResponses.Score.BeatLeaderScore
                    {
                        BeatLeaderScoreId: { } blScoreId
                    }
                        ? $" [Replay](https://replay.beatleader.com/?scoreId={blScoreId})"
                        : null);

                mapContainerBuilder.WithTextDisplay(sb.ToString());
            }
        }

        return mapContainerBuilder;
    }
}