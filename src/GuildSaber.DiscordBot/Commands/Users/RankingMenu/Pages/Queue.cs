using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.MapRequests.Http;
using GuildSaber.Api.Shared;
using GuildSaber.DiscordBot.Core.Extensions;
using static GuildSaber.Api.Features.MapRequests.Http.MapProposalRequests;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking_menu_queue:*,*,*,*")]
    public async Task UpdateCurrentMessageToRankingQueuePagedMenu(
        ContextId contextId, int page, EMapRequestSorter sortBy, ERankingThreadOrigin origin)
    {
        await DeferRankingInteractionAsync();

        var filter = new Filters(IsBoostedByMe: origin == ERankingThreadOrigin.Boosts ? true : null);
        var sorter = new PaginatedRequestOptions<EMapRequestSorter>
        {
            Page = Math.Max(1, page),
            PageSize = 2,
            SortBy = sortBy,
            Order = EOrder.Desc
        };

        if (!(await Client.Value.MapRequests.GetQueueAsync(contextId, filter, sorter))
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

        var component = RankingMenuView.BuildComponent(submittedThreads.Data, submittedThreads.Page, sortBy, origin)
            .WithTextDisplay(
                $"Page: **{submittedThreads.Page}**/{Math.Max(1, submittedThreads.TotalPages)} " +
                $"(**{submittedThreads.TotalCount}** Threads)")
            .WithActionRow(x => x.WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId($"ranking_menu_queue_sort:{contextId},{(int)origin}")
                .WithPlaceholder(sortBy == EMapRequestSorter.BoostCount ? "Most boosted" : "Priority")
                .AddOption("Priority", nameof(EMapRequestSorter.Priority),
                    emote: new Emoji("👀"), isDefault: sortBy == EMapRequestSorter.Priority)
                .AddOption("Most boosted", nameof(EMapRequestSorter.BoostCount),
                    emote: new Emoji("🪙"), isDefault: sortBy == EMapRequestSorter.BoostCount)))
            .WithActionRow(x => x
                .WithButton(label: "Back to menu", customId: $"ranking:{contextId}", style: ButtonStyle.Secondary)
                .WithButton(
                    label: "Prev",
                    disabled: !submittedThreads.HasPreviousPage,
                    customId: $"ranking_menu_queue:{contextId},{submittedThreads.Page - 1},{(int)sortBy},{(int)origin}",
                    style: ButtonStyle.Primary)
                .WithButton(
                    label: "Next",
                    disabled: !submittedThreads.HasNextPage,
                    customId: $"ranking_menu_queue:{contextId},{submittedThreads.Page + 1},{(int)sortBy},{(int)origin}",
                    style: ButtonStyle.Primary))
            .Build();

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
        MapRequestResponses.ThreadPreview[] submittedThreads, int page, EMapRequestSorter sortBy,
        UserModuleSlash.ERankingThreadOrigin origin)
    {
        var builder = new ComponentBuilderV2().WithTextDisplay(
            origin == UserModuleSlash.ERankingThreadOrigin.Boosts ? "### My boosted threads" : "### Ranking queue");

        if (submittedThreads.Length == 0)
            return builder.WithTextDisplay(origin == UserModuleSlash.ERankingThreadOrigin.Boosts
                ? "You have no boosted threads in this context. Boost an open thread from the queue to find it here."
                : "No open proposals in the queue.");

        foreach (var thread in submittedThreads)
            builder.WithContainer(thread.ToContainerBuilder(page, sortBy, origin));

        return builder;
    }
}