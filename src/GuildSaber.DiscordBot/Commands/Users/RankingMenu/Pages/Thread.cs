using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.Api.Features.Players.Http;
using GuildSaber.Common.Helpers;
using GuildSaber.DiscordBot.Core.Extensions;
using static GuildSaber.Api.Features.MapRequests.Http.MapProposalRequests;
using static GuildSaber.Api.Features.MapRequests.Http.MapRequestResponses;
using ThreadResponse = GuildSaber.Api.Features.MapRequests.Http.MapRequestResponses.Thread;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    public enum ERankingThreadTab { Open, History, Drafts, Current }

    public enum ERankingThreadOrigin { Queue, Drafts, Boosts, Search, Matches }

    [ComponentInteraction("ranking_map_thread:*,*")]
    public async Task OpenRankedMapThread(ContextId contextId, MapRequestThreadId threadId)
    {
        await ((SocketMessageComponent)Context.Interaction).DeferLoadingAsync(ephemeral: true);
        await ShowRankingThreadAsync(contextId, threadId,
            new RankingThreadNavigation(ERankingThreadTab.Current, 1, 1, 1, EMapRequestSorter.Priority,
                ERankingThreadOrigin.Search));
    }

    [ComponentInteraction("ranking_back:*,*,*,*")]
    public async Task ReturnToRankingListAsync(
        ContextId contextId, int page, EMapRequestSorter sortBy, ERankingThreadOrigin origin)
    {
        await DeferRankingInteractionAsync();

        await (origin switch
        {
            ERankingThreadOrigin.Drafts => ShowMyRankingDraftsAsync(contextId, page),
            ERankingThreadOrigin.Search or ERankingThreadOrigin.Matches => DeleteOriginalResponseAsync(),
            _ => UpdateCurrentMessageToRankingQueuePagedMenu(contextId, page, sortBy, origin)
        });
    }

    [ComponentInteraction("ranking_thread:*,*,*,*,*,*,*,*")]
    public Task OpenRankingThread(
        ContextId contextId, MapRequestThreadId threadId, ERankingThreadTab tab, int page, int versionPage,
        int listPage, EMapRequestSorter sortBy, ERankingThreadOrigin origin)
        => ShowRankingThreadAsync(contextId, threadId,
            new RankingThreadNavigation(tab, page, versionPage, listPage, sortBy, origin));

    [ComponentInteraction("ranking_thread_tab:*,*,*,*,*")]
    public Task ChangeRankingThreadTab(
        ContextId contextId, MapRequestThreadId threadId, int listPage, EMapRequestSorter sortBy,
        ERankingThreadOrigin origin, string[] values)
        => ShowRankingThreadAsync(contextId, threadId,
            new RankingThreadNavigation(Enum.Parse<ERankingThreadTab>(values[0]), 1, 1, listPage, sortBy, origin));

    [ComponentInteraction("ranking_pick:*,*,*,*,*,*")]
    public Task OpenRankingProposal(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, int listPage,
        EMapRequestSorter sortBy, ERankingThreadOrigin origin)
        => ShowRankingThreadAsync(contextId, threadId,
            new RankingThreadNavigation(ERankingThreadTab.Open, 1, 1, listPage, sortBy, origin), proposalId);

    private async Task ShowRankingThreadAsync(
        ContextId contextId, MapRequestThreadId threadId, RankingThreadNavigation navigation,
        MapProposalId? selectedProposalId = null)
    {
        await DeferRankingInteractionAsync();

        var guildId = await GetGuildIdAsync();
        var (threadResult, viewerResult, allowanceResult) = await (
            Client.Value.MapRequests.GetThreadAsync(contextId, threadId),
            Client.Value.Players.GetExtendedAtMeAsync(),
            Client.Value.MapRequests.GetAllowanceAsync(contextId)
        ).WhenAll();

        if (threadResult.TryGetError(out var error))
        {
            await ReportRankingErrorAsync(error);
            return;
        }

        if (threadResult.Value is not { } thread)
        {
            await ReturnToRankingListAsync(contextId, navigation.ListPage, navigation.SortBy, navigation.Origin);
            await FollowupAsync("This thread is no longer available. Your list has been refreshed.", ephemeral: true);
            return;
        }

        if (viewerResult.TryGetError(out var viewerError))
        {
            await FollowupAsync($"Your player profile could not be loaded. Please refresh.\n{viewerError}",
                ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        if (selectedProposalId is { } id && thread.Proposals.FirstOrDefault(x => x.Id == id) is { } selected)
            navigation = navigation with
            {
                Tab = selected switch
                {
                    Proposal.Draft => ERankingThreadTab.Drafts,
                    Proposal.Submitted => ERankingThreadTab.Open,
                    Proposal.Accepted when RankingThreadView.GetCurrentProposal(thread)?.Id == selected.Id
                        => ERankingThreadTab.Current,
                    _ => ERankingThreadTab.History
                }
            };

        var proposals = RankingThreadView.GetProposals(thread, navigation.Tab).ToArray();
        navigation = navigation with
        {
            Page = Math.Clamp(selectedProposalId is { } proposalId
                    ? Math.Max(1, Array.FindIndex(proposals, x => x.Id == proposalId) + 1)
                    : navigation.Page,
                1, Math.Max(1, proposals.Length))
        };
        navigation = navigation with
        {
            VersionPage = Math.Clamp(navigation.VersionPage, 1,
                proposals.Length == 0 ? 1 : proposals[navigation.Page - 1].Definition.MapVersions.Length)
        };

        var reviewers = proposals.Length == 0
            ? []
            : await Task.WhenAll(proposals[navigation.Page - 1].Votes.OfType<Vote.RankingTeam>()
                .Where(x => x.Reason is { Length: > 0 })
                .OrderByDescending(x => x.VotedAt).Take(3)
                .Select(x => Cache.GetPlayerByIdAsync(x.PlayerId, Client.Value).AsTask()));

        var component = RankingThreadView.BuildComponent(guildId, thread, proposals,
            viewerResult.GetValueOrDefault(), reviewers, allowanceResult.GetValueOrDefault(), navigation).Build();

        await ModifyOriginalResponseAsync(x =>
        {
            x.Flags = MessageFlags.ComponentsV2;
            x.Components = component;
            x.AllowedMentions = AllowedMentions.None;
        });
    }

    private Task DeferRankingInteractionAsync()
        => Context.Interaction.HasResponded ? Task.CompletedTask : DeferAsync();

    private Task ReportRankingErrorAsync(Error error)
        => FollowupAsync(error.Errors is { Count: > 0 } errors
            ? string.Join("\n", errors.SelectMany(x => x.Value).Distinct().Take(10))
            : error.Code switch
            {
                "vote_requirements" => $"Acceptance needs **{error.RequiredPositiveVotes}** positive RT votes" +
                                       (error.RequireNoNegativeVotes is true ? " and no negative RT votes" : "") +
                                       $". Currently **{error.PositiveVotes}** positive and **{error.NegativeVotes}** negative.",
                "invalid_state" => "This proposal has already changed state. Its current state is shown below.",
                _ => error.Detail ?? error.Title ?? "The request could not be completed. Please try again."
            }, ephemeral: true, allowedMentions: AllowedMentions.None);
}

public readonly record struct RankingThreadNavigation(
    UserModuleSlash.ERankingThreadTab Tab,
    int Page,
    int VersionPage,
    int ListPage,
    EMapRequestSorter SortBy,
    UserModuleSlash.ERankingThreadOrigin Origin = UserModuleSlash.ERankingThreadOrigin.Queue)
{
    public string CustomId(ContextId contextId, MapRequestThreadId threadId)
        => $"ranking_thread:{contextId},{threadId},{(int)Tab},{Page},{VersionPage},{ListPage},{(int)SortBy},{(int)Origin}";
}

file static class RankingThreadView
{
    public static Proposal.Accepted? GetCurrentProposal(ThreadResponse thread)
        => thread.RankedMapId is null
            ? null
            : thread.Proposals.OfType<Proposal.Accepted>()
                .OrderByDescending(x => x.AcceptedAt).ThenByDescending(x => x.Id.Value).FirstOrDefault();

    public static IEnumerable<Proposal> GetProposals(ThreadResponse thread, UserModuleSlash.ERankingThreadTab tab)
        => thread.Proposals.Where(x => tab switch
            {
                UserModuleSlash.ERankingThreadTab.Open => x is Proposal.Submitted,
                UserModuleSlash.ERankingThreadTab.Current => x.Id == GetCurrentProposal(thread)?.Id,
                UserModuleSlash.ERankingThreadTab.History => x is Proposal.Accepted or Proposal.Closed,
                UserModuleSlash.ERankingThreadTab.Drafts => x is Proposal.Draft,
                _ => false
            })
            .OrderByDescending(x => tab == UserModuleSlash.ERankingThreadTab.Open
                                    && thread.AuthorId != null && x.AuthorId == thread.AuthorId)
            .ThenByDescending(x => tab == UserModuleSlash.ERankingThreadTab.History ? x.EventTime : x.CreatedAt)
            .ThenByDescending(x => x.Id.Value);

    public static ComponentBuilderV2 BuildComponent(
        GuildId guildId, ThreadResponse thread, Proposal[] proposals, PlayerResponses.PlayerExtended? viewer,
        PlayerResponses.Player?[] reviewers, Allowance? allowance, RankingThreadNavigation navigation)
    {
        var builder = new ComponentBuilderV2()
            .WithContainer(container => container.WithAccentColor(Color.Red)
                .WithSection(section => section
                    .WithTextDisplay(
                        $"### {(thread.RankedMapId is null ? "New ranking" : "Ranked-map changes")} · Thread #{thread.Id}\n" +
                        $"**{thread.Proposals.OfType<Proposal.Submitted>().Count()}** open proposals · **{thread.BoostCount}** boosts")
                    .WithAccessory(new ButtonBuilder()
                        .WithLabel(thread.IsBoostedByMe ? "Release my boost" : "Boost thread")
                        .WithCustomId(
                            $"ranking_boost:{thread.ContextId},{thread.Id},{!thread.IsBoostedByMe},{navigation.ListPage},{(int)navigation.SortBy},{(int)navigation.Origin},{(int)navigation.Tab},{navigation.Page},{navigation.VersionPage}")
                        .WithStyle(thread.IsBoostedByMe ? ButtonStyle.Success : ButtonStyle.Secondary)
                        .WithDisabled(viewer is null || !thread.IsBoostedByMe
                            && !thread.Proposals.Any(x => x is Proposal.Submitted))))
                .WithActionRow(row =>
                {
                    var menu = new SelectMenuBuilder()
                        .WithCustomId(
                            $"ranking_thread_tab:{thread.ContextId},{thread.Id},{navigation.ListPage},{(int)navigation.SortBy},{(int)navigation.Origin}")
                        .WithPlaceholder("Choose proposals to view");

                    if (GetCurrentProposal(thread) is not null)
                        menu.AddOption("Current ranked map", "Current",
                            isDefault: navigation.Tab == UserModuleSlash.ERankingThreadTab.Current);

                    menu.AddOption($"Open proposals ({thread.Proposals.OfType<Proposal.Submitted>().Count()})", "Open",
                            isDefault: navigation.Tab == UserModuleSlash.ERankingThreadTab.Open)
                        .AddOption(
                            $"History ({thread.Proposals.Count(x => x is Proposal.Accepted or Proposal.Closed)})",
                            "History",
                            isDefault: navigation.Tab == UserModuleSlash.ERankingThreadTab.History)
                        .AddOption($"My private drafts ({thread.Proposals.OfType<Proposal.Draft>().Count()})", "Drafts",
                            isDefault: navigation.Tab == UserModuleSlash.ERankingThreadTab.Drafts);

                    row.WithSelectMenu(menu);
                }));

        if (proposals.Length == 0)
        {
            builder.WithTextDisplay(navigation.Tab switch
            {
                UserModuleSlash.ERankingThreadTab.Drafts => "You have no private drafts in this thread.",
                UserModuleSlash.ERankingThreadTab.History => "No proposals have been accepted or closed yet.",
                UserModuleSlash.ERankingThreadTab.Current => "There is no current accepted proposal.",
                _ => GetCurrentProposal(thread) is not null
                    ? "There are no open proposals. Choose Current ranked map to fork its accepted definition."
                    : "There are no open proposals. Choose History to fork a previous definition."
            });
        }
        else
        {
            var proposal = proposals[navigation.Page - 1];
            var current = GetCurrentProposal(thread);

            builder.WithContainer(proposal.ToContainerBuilder(current, navigation.VersionPage))
                .WithTextDisplay($"Proposal **{navigation.Page}**/{proposals.Length} · " +
                                 $"Map version **{navigation.VersionPage}**/{proposal.Definition.MapVersions.Length}");

            if (proposal.Definition.MapVersions.Length > 1)
                builder.WithActionRow(row => row
                    .WithButton("Previous map", (navigation with { VersionPage = navigation.VersionPage - 1 })
                        .CustomId(thread.ContextId, thread.Id), ButtonStyle.Secondary,
                        disabled: navigation.VersionPage == 1)
                    .WithButton("Next map", (navigation with { VersionPage = navigation.VersionPage + 1 })
                        .CustomId(thread.ContextId, thread.Id), ButtonStyle.Secondary,
                        disabled: navigation.VersionPage == proposal.Definition.MapVersions.Length));

            RankingProposalActions.AddComponents(builder, thread, proposal, viewer, reviewers, guildId, allowance,
                navigation);
        }

        return builder.WithActionRow(row => row
            .WithButton(navigation.Origin switch
                {
                    UserModuleSlash.ERankingThreadOrigin.Drafts => "Back to my drafts",
                    UserModuleSlash.ERankingThreadOrigin.Boosts => "Back to my boosts",
                    UserModuleSlash.ERankingThreadOrigin.Search => "Back to search",
                    UserModuleSlash.ERankingThreadOrigin.Matches => "Back to map matches",
                    _ => "Back to queue"
                },
                $"ranking_back:{thread.ContextId},{navigation.ListPage},{(int)navigation.SortBy},{(int)navigation.Origin}",
                ButtonStyle.Secondary)
            .WithButton("Prev", (navigation with { Page = navigation.Page - 1, VersionPage = 1 })
                .CustomId(thread.ContextId, thread.Id), disabled: navigation.Page <= 1)
            .WithButton("Next", (navigation with { Page = navigation.Page + 1, VersionPage = 1 })
                .CustomId(thread.ContextId, thread.Id),
                disabled: navigation.Page >= proposals.Length)
            .WithButton("Refresh", navigation.CustomId(thread.ContextId, thread.Id), ButtonStyle.Secondary));
    }
}