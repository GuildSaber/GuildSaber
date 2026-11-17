using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.MapRequests.Http;
using GuildSaber.Api.Shared;
using GuildSaber.Common.Helpers;
using GuildSaber.DiscordBot.Core.Extensions;
using static GuildSaber.Api.Features.MapRequests.Http.MapProposalRequests;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking_menu_queue:*,*,*,*")]
    public async Task UpdateCurrentMessageToRankingQueuePagedMenu(
        ContextId contextId, int page, EMapRequestSorter sortBy, ERankingThreadOrigin origin)
    {
        var filter = new Filters(IsBoostedByMe: origin == ERankingThreadOrigin.Boosts ? true : null);
        var sorter = new PaginatedRequestOptions<EMapRequestSorter>
        {
            Page = Math.Max(1, page),
            PageSize = 2,
            SortBy = sortBy,
            Order = EOrder.Desc
        };

        if (!(await (
                DeferRankingInteractionAsync(),
                Client.Value.MapRequests.GetQueueAsync(contextId, filter, sorter)
            ).WhenAll())
            .TryGetValue(out var submittedThreads, out var error))
        {
            await ReportRankingErrorAsync(error);
            return;
        }

        if (submittedThreads.Page > Math.Max(1, submittedThreads.TotalPages))
        {
            await UpdateCurrentMessageToRankingQueuePagedMenu(contextId, Math.Max(1, submittedThreads.TotalPages),
                sortBy, origin);
            return;
        }

        var component = RankingMenuView.BuildComponent(contextId, submittedThreads, sortBy, origin).Build();

        await ModifyOriginalResponseAsync(x =>
        {
            x.Flags = MessageFlags.ComponentsV2;
            x.Components = component;
            x.AllowedMentions = AllowedMentions.None;
        });
    }

    [ComponentInteraction("ranking_menu_queue_sort:*,*")]
    public Task SortRankingQueue(ContextId contextId, ERankingThreadOrigin origin, string[] values)
        => UpdateCurrentMessageToRankingQueuePagedMenu(contextId, 1, Enum.Parse<EMapRequestSorter>(values[0]), origin);
}

file static class RankingMenuView
{
    public static ComponentBuilderV2 BuildComponent(
        ContextId contextId, PagedList<MapRequestResponses.ThreadPreview> submittedThreads, EMapRequestSorter sortBy,
        UserModuleSlash.ERankingThreadOrigin origin)
    {
        var builder = new ComponentBuilderV2().WithTextDisplay(
            origin == UserModuleSlash.ERankingThreadOrigin.Boosts ? "### My boosted threads" : "### Ranking queue");

        if (submittedThreads.TotalCount > 1)
            builder.WithActionRow(row => row.WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId($"ranking_menu_queue_sort:{contextId},{(int)origin}")
                .WithPlaceholder(sortBy == EMapRequestSorter.BoostCount ? "Most boosted" : "Priority")
                .AddOption("Priority", nameof(EMapRequestSorter.Priority),
                    emote: new Emoji("👀"), isDefault: sortBy == EMapRequestSorter.Priority)
                .AddOption("Most boosted", nameof(EMapRequestSorter.BoostCount),
                    emote: new Emoji("🪙"), isDefault: sortBy == EMapRequestSorter.BoostCount)));

        if (submittedThreads.Data.Length == 0)
            builder.WithTextDisplay(origin == UserModuleSlash.ERankingThreadOrigin.Boosts
                ? "No boosted threads in this context. Boost a thread from the queue."
                : "No open proposals in the queue.");

        foreach (var thread in submittedThreads.Data)
            builder.WithContainer(thread.ToContainerBuilder(submittedThreads.Page, sortBy, origin));

        if (submittedThreads.TotalPages > 1)
            builder.WithTextDisplay($"-# Page {submittedThreads.Page}/{submittedThreads.TotalPages}");

        return builder.WithActionRow(row =>
        {
            row.WithButton("Back to menu", $"ranking:{contextId}", ButtonStyle.Secondary);

            if (submittedThreads.TotalPages > 1)
                row.WithButton("Prev",
                        $"ranking_menu_queue:{contextId},{submittedThreads.Page - 1},{(int)sortBy},{(int)origin}",
                        disabled: !submittedThreads.HasPreviousPage)
                    .WithButton("Next",
                        $"ranking_menu_queue:{contextId},{submittedThreads.Page + 1},{(int)sortBy},{(int)origin}",
                        disabled: !submittedThreads.HasNextPage);

            row.WithButton("Refresh",
                $"ranking_menu_queue:{contextId},{submittedThreads.Page},{(int)sortBy},{(int)origin}",
                ButtonStyle.Secondary);
        });
    }
}