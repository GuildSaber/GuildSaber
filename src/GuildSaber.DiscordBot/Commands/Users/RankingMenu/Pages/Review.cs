using System.Text;
using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Members.Http;
using GuildSaber.Api.Features.Players.Http;
using GuildSaber.Common.Helpers;
using static GuildSaber.Api.Features.MapRequests.Http.MapProposalRequests;
using static GuildSaber.Api.Features.MapRequests.Http.MapRequestResponses;
using static GuildSaber.DiscordBot.Commands.Users.UserModuleSlash;
using ThreadResponse = GuildSaber.Api.Features.MapRequests.Http.MapRequestResponses.Thread;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    public enum ERankingProposalAction
    {
        Support,
        Oppose,
        RemoveVote,
        SupportRT,
        OpposeRT,
        RemoveRTVote,
        Accept,
        Deny,
        Submit,
        Withdraw,
        DeleteDraft,
        ForceSubmit
    }

    [ComponentInteraction("ranking_vote:*,*,*,*,*,*,*")]
    public async Task VoteOnRankingProposal(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, ERankingProposalAction action,
        int listPage, EMapRequestSorter sortBy, ERankingThreadOrigin origin)
    {
        var result = await (
            DeferRankingInteractionAsync(),
            action switch
            {
                ERankingProposalAction.Support => Client.Value.MapRequests.VoteCommunityAsync(
                    contextId, threadId, proposalId, new CommunityVote(true)),
                ERankingProposalAction.Oppose => Client.Value.MapRequests.VoteCommunityAsync(
                    contextId, threadId, proposalId, new CommunityVote(false)),
                ERankingProposalAction.RemoveVote => Client.Value.MapRequests.RemoveCommunityVoteAsync(
                    contextId, threadId, proposalId),
                ERankingProposalAction.RemoveRTVote => Client.Value.MapRequests.RemoveRankingTeamVoteAsync(
                    contextId, threadId, proposalId),
                _ => throw new ArgumentOutOfRangeException(nameof(action))
            }
        ).WhenAll();

        await OpenRankingProposal(contextId, threadId, proposalId, listPage, sortBy, origin);
        if (result.TryGetError(out var error)) await ReportRankingErrorAsync(error);
    }

    [ComponentInteraction("ranking_reason:*,*,*,*,*,*,*")]
    public Task ShowRankingProposalReason(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, ERankingProposalAction action,
        int listPage, EMapRequestSorter sortBy, ERankingThreadOrigin origin)
        => RespondWithModalAsync<RankingProposalReasonModal>(
            $"ranking_reason_submit:{contextId},{threadId},{proposalId},{(int)action},{listPage},{(int)sortBy},{(int)origin}",
            modifyModal: modal => modal.WithTitle(action switch
            {
                ERankingProposalAction.SupportRT => "Support proposal as RT",
                ERankingProposalAction.OpposeRT => "Oppose proposal as RT",
                ERankingProposalAction.Deny => "Deny proposal",
                ERankingProposalAction.Withdraw => "Withdraw your proposal",
                _ => throw new ArgumentOutOfRangeException(nameof(action))
            }));

    [ModalInteraction("ranking_reason_submit:*,*,*,*,*,*,*")]
    public async Task ApplyRankingProposalReason(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, ERankingProposalAction action,
        int listPage, EMapRequestSorter sortBy, ERankingThreadOrigin origin, RankingProposalReasonModal modal)
    {
        var reason = string.IsNullOrWhiteSpace(modal.Reason) ? null : modal.Reason.Trim();
        var error = await (
            DeferRankingInteractionAsync(),
            action switch
            {
                ERankingProposalAction.SupportRT => GetRankingErrorAsync(Client.Value.MapRequests.VoteRankingTeamAsync(
                    contextId, threadId, proposalId, new RankingTeamVote(true, reason))),
                ERankingProposalAction.OpposeRT => GetRankingErrorAsync(Client.Value.MapRequests.VoteRankingTeamAsync(
                    contextId, threadId, proposalId, new RankingTeamVote(false, reason))),
                ERankingProposalAction.Deny => GetRankingErrorAsync(Client.Value.MapRequests.DenyAsync(
                    contextId, threadId, proposalId, reason)),
                ERankingProposalAction.Withdraw => GetRankingErrorAsync(Client.Value.MapRequests.CloseAsync(
                    contextId, threadId, proposalId, reason)),
                _ => throw new ArgumentOutOfRangeException(nameof(action))
            }
        ).WhenAll();

        await OpenRankingProposal(contextId, threadId, proposalId, listPage, sortBy, origin);
        if (error is not null) await ReportRankingErrorAsync(error);
    }

    [ComponentInteraction("ranking_confirm:*,*,*,*,*,*,*")]
    public async Task ConfirmRankingProposalAction(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, ERankingProposalAction action,
        int listPage, EMapRequestSorter sortBy, ERankingThreadOrigin origin)
    {
        var result = await (
            DeferRankingInteractionAsync(),
            Client.Value.MapRequests.GetProposalAsync(contextId, threadId, proposalId)
        ).WhenAll();
        if (result.TryGetError(out var error))
        {
            await ReportRankingErrorAsync(error);
            return;
        }

        if (result.Value is not { } proposal)
        {
            await OpenRankingProposal(contextId, threadId, proposalId, listPage, sortBy, origin);
            return;
        }

        await ModifyOriginalResponseAsync(x =>
        {
            x.Components = RankingProposalActions
                .BuildConfirmation(contextId, threadId, proposal, action, listPage, sortBy, origin).Build();
            x.AllowedMentions = AllowedMentions.None;
        });
    }

    [ComponentInteraction("ranking_apply:*,*,*,*,*,*,*")]
    public async Task ApplyRankingProposalAction(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, ERankingProposalAction action,
        int listPage, EMapRequestSorter sortBy, ERankingThreadOrigin origin)
    {
        var error = await (
            DeferRankingInteractionAsync(),
            action switch
            {
                ERankingProposalAction.Accept => GetRankingErrorAsync(Client.Value.MapRequests.AcceptAsync(
                    contextId, threadId, proposalId)),
                ERankingProposalAction.Submit or ERankingProposalAction.ForceSubmit
                    => GetRankingErrorAsync(Client.Value.MapRequests.SubmitAsync(
                        contextId, threadId, proposalId, force: action == ERankingProposalAction.ForceSubmit)),
                ERankingProposalAction.DeleteDraft => GetRankingErrorAsync(Client.Value.MapRequests.DeleteDraftAsync(
                    contextId, threadId, proposalId)),
                _ => throw new ArgumentOutOfRangeException(nameof(action))
            }
        ).WhenAll();

        if (error is { Code: "duplicate_maps" })
        {
            await ModifyOriginalResponseAsync(x => x.Components = new ComponentBuilderV2()
                .WithContainer(container => container.WithAccentColor(Color.Orange)
                    .WithTextDisplay("### This difficulty already appears elsewhere\n" +
                                     "Check these existing requests or ranked maps before submitting a separate ranking:\n" +
                                     string.Join("\n", (error.Duplicates ?? []).Select(match =>
                                         $"Thread #{match.ThreadId}" + (match.RankedMapId is { } mapId
                                             ? $" · ranked map #{mapId}"
                                             : ""))))
                    .WithActionRow(row => row
                        .WithButton("Review separate submission",
                            $"ranking_confirm:{contextId},{threadId},{proposalId},{(int)ERankingProposalAction.ForceSubmit},{listPage},{(int)sortBy},{(int)origin}",
                            ButtonStyle.Danger)
                        .WithButton("Back to draft",
                            $"ranking_pick:{contextId},{threadId},{proposalId},{listPage},{(int)sortBy},{(int)origin}",
                            ButtonStyle.Secondary)))
                .Build());
            return;
        }

        await OpenRankingProposal(contextId, threadId, proposalId, listPage, sortBy, origin);
        if (error is not null) await ReportRankingErrorAsync(error);
        else
            await FollowupAsync(action switch
            {
                ERankingProposalAction.Accept =>
                    "Proposal accepted. Its requested change was applied and member stats refresh in the background.",
                ERankingProposalAction.DeleteDraft => "Your private draft has been deleted.",
                _ => "Your proposal is now open for review."
            }, ephemeral: true);
    }

    [ComponentInteraction("ranking_boost:*,*,*,*,*,*,*,*,*")]
    public async Task SetRankingThreadBoost(
        ContextId contextId, MapRequestThreadId threadId, bool boost, int listPage, EMapRequestSorter sortBy,
        ERankingThreadOrigin origin, ERankingThreadTab tab, int page, int versionPage)
    {
        var result = await (
            DeferRankingInteractionAsync(),
            boost
                ? Client.Value.MapRequests.BoostAsync(contextId, threadId)
                : Client.Value.MapRequests.RemoveBoostAsync(contextId, threadId)
        ).WhenAll();

        await ShowRankingThreadAsync(contextId, threadId,
            new RankingThreadNavigation(tab, page, versionPage, listPage, sortBy, origin));
        if (result.TryGetError(out var error)) await ReportRankingErrorAsync(error);
    }

    private static async Task<Error?> GetRankingErrorAsync<T>(Task<Result<T, Error>> operation)
        => (await operation).TryGetError(out var error) ? error : null;

    private static async Task<Error?> GetRankingErrorAsync(Task<UnitResult<Error>> operation)
        => (await operation).TryGetError(out var error) ? error : null;
}

public class RankingProposalReasonModal : IModal
{
    public string Title => "Proposal review";

    [InputLabel("Reason", "Optional explanation retained with your vote or decision.")]
    [ModalTextInput("reason", TextInputStyle.Paragraph, maxLength: 1000)]
    [RequiredInput(false)]
    public string? Reason { get; set; }
}

internal static class RankingProposalActions
{
    public static ComponentBuilderV2 AddComponents(
        ComponentBuilderV2 builder, ThreadResponse thread, Proposal proposal, PlayerResponses.PlayerExtended? viewer,
        PlayerResponses.Player?[] reviewers, GuildId guildId, Allowance? allowance, RankingThreadNavigation navigation)
    {
        var community = proposal.Votes.OfType<Vote.Community>();
        var rankingTeam = proposal.Votes.OfType<Vote.RankingTeam>();
        var permission = viewer?.Members.FirstOrDefault(x => x.GuildId == guildId)?.Permissions
                         ?? MemberResponses.EPermission.None;
        var leader = viewer?.Player.IsManager is true || permission.HasFlag(MemberResponses.EPermission.GuildLeader);
        var reviewer = leader || permission.HasFlag(MemberResponses.EPermission.RankingTeam);

        var sb = new StringBuilder()
            .AppendLine(
                $"**Community:** +{community.Count(x => x.IsPositive)} / -{community.Count(x => !x.IsPositive)}")
            .AppendLine(
                $"**Ranking team:** +{rankingTeam.Count(x => x.IsPositive)} / -{rankingTeam.Count(x => !x.IsPositive)}");

        foreach (var vote in rankingTeam.Where(x => x.Reason is { Length: > 0 })
                     .OrderByDescending(x => x.VotedAt).Take(3))
            sb.Append("RT ").Append(vote.IsPositive ? "support" : "opposition")
                .Append(" · ").Append(reviewers.FirstOrDefault(x => x?.Id == vote.PlayerId) is { } player
                    ? Format.Sanitize(player.PlayerInfo.Username)
                    : $"player #{vote.PlayerId}")
                .Append(": ")
                .AppendLine(vote.Reason is { Length: > 200 } reason ? $"{reason[..200]}..." : vote.Reason);

        if (proposal is Proposal.Submitted && reviewer)
            sb.AppendLine().Append(allowance is null
                ? "Review rules could not be loaded. Refresh before accepting."
                : $"Acceptance requires **{allowance.MinimumPositiveVotes}** positive RT votes" +
                  (allowance.RequireNoNegativeVotes ? " and **no negative RT votes**." : ".") +
                  (leader
                      ? " You can bypass the vote requirement as a leader/manager."
                      : " RT votes do not accept a proposal automatically."));

        builder.WithTextDisplay(sb.ToString());

        if (viewer is null)
            return builder.WithTextDisplay("Link your Discord account on GuildSaber to vote or manage your proposals.");

        if (proposal is not Proposal.Draft)
        {
            var unavailable = allowance switch
            {
                null => "Your request limits could not be loaded. Refresh before forking.",
                not null when allowance.Drafts >= allowance.MaxDrafts =>
                    "Your private drafts are full. Delete a draft or submit one before saving a fork.",
                not null when !reviewer && (int)viewer.Player.PlayerSubscriptionInfo.Tier <
                                        allowance.MinimumAmendmentTier
                                        && thread.Proposals.Any(x
                                            => x is Proposal.Submitted && x.AuthorId != viewer.Player.Id) =>
                    $"This context requires subscription tier {allowance.MinimumAmendmentTier} to propose a competing variant.",
                _ => null
            };

            builder.WithActionRow(row => row.WithButton(
                proposal.Change is ProposalChange.Unrank ? "Draft a ranking" : "Fork as draft",
                $"ranking_fork:{thread.ContextId},{thread.Id},{proposal.Id},{navigation.ListPage},{(int)navigation.SortBy},{(int)navigation.Origin}",
                disabled: unavailable is not null));

            if (unavailable is not null) builder.WithTextDisplay(unavailable);
        }

        if (proposal is Proposal.Submitted)
        {
            builder.WithActionRow(row => row
                .WithButton("Support",
                    ActionId("ranking_vote", thread, proposal, ERankingProposalAction.Support, navigation),
                    community.Any(x => x.PlayerId == viewer.Player.Id && x.IsPositive)
                        ? ButtonStyle.Success
                        : ButtonStyle.Secondary)
                .WithButton("Oppose",
                    ActionId("ranking_vote", thread, proposal, ERankingProposalAction.Oppose, navigation),
                    community.Any(x => x.PlayerId == viewer.Player.Id && !x.IsPositive)
                        ? ButtonStyle.Danger
                        : ButtonStyle.Secondary)
                .WithButton("Remove my vote",
                    ActionId("ranking_vote", thread, proposal, ERankingProposalAction.RemoveVote, navigation),
                    ButtonStyle.Secondary, disabled: !community.Any(x => x.PlayerId == viewer.Player.Id)));

            if (reviewer)
                builder.WithActionRow(row => row
                        .WithButton("Support (RT)",
                            ActionId("ranking_reason", thread, proposal, ERankingProposalAction.SupportRT, navigation),
                            rankingTeam.Any(x => x.PlayerId == viewer.Player.Id && x.IsPositive)
                                ? ButtonStyle.Success
                                : ButtonStyle.Secondary)
                        .WithButton("Oppose (RT)",
                            ActionId("ranking_reason", thread, proposal, ERankingProposalAction.OpposeRT, navigation),
                            rankingTeam.Any(x => x.PlayerId == viewer.Player.Id && !x.IsPositive)
                                ? ButtonStyle.Danger
                                : ButtonStyle.Secondary)
                        .WithButton("Remove RT vote",
                            ActionId("ranking_vote", thread, proposal, ERankingProposalAction.RemoveRTVote, navigation),
                            ButtonStyle.Secondary, disabled: !rankingTeam.Any(x => x.PlayerId == viewer.Player.Id)))
                    .WithActionRow(row =>
                    {
                        row.WithButton("Accept proposal",
                                ActionId("ranking_confirm", thread, proposal, ERankingProposalAction.Accept,
                                    navigation),
                                ButtonStyle.Success, disabled: !leader && (allowance is null
                                                                           || rankingTeam.Count(x => x.IsPositive) <
                                                                           allowance.MinimumPositiveVotes
                                                                           || allowance.RequireNoNegativeVotes &&
                                                                           rankingTeam.Any(x => !x.IsPositive)))
                            .WithButton("Deny proposal",
                                ActionId("ranking_reason", thread, proposal, ERankingProposalAction.Deny, navigation),
                                ButtonStyle.Danger);

                        if (proposal.AuthorId == viewer.Player.Id)
                            row.WithButton("Withdraw my proposal",
                                ActionId("ranking_reason", thread, proposal, ERankingProposalAction.Withdraw,
                                    navigation),
                                ButtonStyle.Secondary);
                    });
            else
                builder.WithTextDisplay(
                    "Community votes are feedback. Only the ranking team can accept or deny proposals.");

            if (!reviewer && proposal.AuthorId == viewer.Player.Id)
                builder.WithActionRow(row => row.WithButton("Withdraw my proposal",
                    ActionId("ranking_reason", thread, proposal, ERankingProposalAction.Withdraw, navigation),
                    ButtonStyle.Secondary));
        }
        else if (proposal is Proposal.Draft && proposal.AuthorId == viewer.Player.Id)
        {
            builder.WithActionRow(row => row
                .WithButton("Revise draft",
                    $"{(proposal.Change is ProposalChange.Unrank ? "ranking_unrank" : "ranking_draft")}:{thread.ContextId},{thread.Id},{proposal.Id},{navigation.ListPage},{(int)navigation.SortBy},{(int)navigation.Origin}")
                .WithButton("Submit for review",
                    ActionId("ranking_confirm", thread, proposal, ERankingProposalAction.Submit, navigation),
                    ButtonStyle.Success)
                .WithButton("Delete draft",
                    ActionId("ranking_confirm", thread, proposal, ERankingProposalAction.DeleteDraft, navigation),
                    ButtonStyle.Danger));
        }
        else
        {
            builder.WithTextDisplay(
                "Its definition and votes are retained in history. Fork a private draft to suggest a change.");
        }

        return builder;
    }

    public static ComponentBuilderV2 BuildConfirmation(
        ContextId contextId, MapRequestThreadId threadId, Proposal proposal, ERankingProposalAction action,
        int listPage, EMapRequestSorter sortBy, ERankingThreadOrigin origin)
        => new ComponentBuilderV2().WithContainer(container => container
            .WithAccentColor(action == ERankingProposalAction.Accept ? Color.Green : Color.Orange)
            .WithTextDisplay($"### {action switch
            {
                ERankingProposalAction.Accept => proposal.Change is ProposalChange.Unrank
                    ? "Accept unranking?" : "Accept this proposal?",
                ERankingProposalAction.DeleteDraft => "Delete this private draft?",
                ERankingProposalAction.ForceSubmit => "Submit a separate ranking anyway?",
                ERankingProposalAction.Submit => "Publish this proposal?",
                _ => throw new ArgumentOutOfRangeException(nameof(action))
            }}\n**Proposal #{proposal.Id}** · {proposal.GetPreviewDefinition().MapVersions.First().Song.Info.BeatSaverName}\n" +
                             action switch
                             {
                                 ERankingProposalAction.Accept when proposal.Change is ProposalChange.Unrank =>
                                     "This removes the targeted ranked entry and its ranked scores, closes competing open proposals and releases boosts. Member points and achievement progress will be refreshed.",
                                 ERankingProposalAction.Accept =>
                                     "This applies the complete definition to the ranked map, closes competing open proposals and releases boosts. Private drafts stay open.",
                                 ERankingProposalAction.DeleteDraft =>
                                     "Only this draft will be deleted. This cannot be undone.",
                                 ERankingProposalAction.ForceSubmit =>
                                     "You are deliberately requesting a separate ranking despite the duplicate matches. Existing requests and rankings will not be replaced.",
                                 _ =>
                                     proposal.Change is ProposalChange.Unrank
                                         ? "Publish the unranking request and its reason for community and ranking-team review."
                                         : "The proposal becomes immutable and visible for review. Missing ratings will be calculated before submission."
                             })
            .WithActionRow(row => row
                .WithButton(action == ERankingProposalAction.Accept ? "Confirm acceptance" : "Confirm",
                    $"ranking_apply:{contextId},{threadId},{proposal.Id},{(int)action},{listPage},{(int)sortBy},{(int)origin}",
                    action is ERankingProposalAction.DeleteDraft or ERankingProposalAction.ForceSubmit
                    || action == ERankingProposalAction.Accept && proposal.Change is ProposalChange.Unrank
                        ? ButtonStyle.Danger
                        : ButtonStyle.Success)
                .WithButton("Cancel",
                    $"ranking_pick:{contextId},{threadId},{proposal.Id},{listPage},{(int)sortBy},{(int)origin}",
                    ButtonStyle.Secondary)));

    private static string ActionId(
        string prefix, ThreadResponse thread, Proposal proposal, ERankingProposalAction action,
        RankingThreadNavigation navigation)
        =>
            $"{prefix}:{thread.ContextId},{thread.Id},{proposal.Id},{(int)action},{navigation.ListPage},{(int)navigation.SortBy},{(int)navigation.Origin}";
}