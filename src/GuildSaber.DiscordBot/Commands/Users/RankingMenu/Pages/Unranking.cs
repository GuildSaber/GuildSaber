using Discord;
using Discord.Interactions;
using GuildSaber.Common.Helpers;
using static GuildSaber.Api.Features.MapRequests.Http.MapProposalRequests;
using static GuildSaber.Api.Features.MapRequests.Http.MapRequestResponses;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking_unrank:*,*,*,*,*,*")]
    public async Task ShowUnrankingDraftAsync(
        ContextId contextId, MapRequestThreadId threadId, int draftId, int listPage,
        EMapRequestSorter sortBy, ERankingThreadOrigin origin)
    {
        if (draftId == 0)
        {
            await RespondWithModalAsync(
                $"ranking_unrank_save:{contextId},{threadId},{draftId},{listPage},{(int)sortBy},{(int)origin}",
                new RankingUnrankingModal());
            return;
        }

        if (!(await Client.Value.MapRequests.GetProposalAsync(contextId, threadId, new MapProposalId(draftId)))
            .TryGetValue(out var proposal, out var error))
        {
            await RespondAsync(error.Detail ?? error.Title ?? "The draft could not be loaded.", ephemeral: true,
                allowedMentions: AllowedMentions.None);
            return;
        }

        if (proposal is not Proposal.Draft { Change: ProposalChange.Unrank unrank })
        {
            await RespondAsync("This unranking draft is no longer available. Refresh the thread.", ephemeral: true);
            return;
        }

        await RespondWithModalAsync(
            $"ranking_unrank_save:{contextId},{threadId},{draftId},{listPage},{(int)sortBy},{(int)origin}",
            new RankingUnrankingModal { Reason = unrank.Reason });
    }

    [ModalInteraction("ranking_unrank_save:*,*,*,*,*,*")]
    public async Task SaveUnrankingDraftAsync(
        ContextId contextId, MapRequestThreadId threadId, int draftId, int listPage,
        EMapRequestSorter sortBy, ERankingThreadOrigin origin, RankingUnrankingModal modal)
    {
        if (!(await (
                DeferRankingInteractionAsync(),
                draftId == 0
                    ? Client.Value.MapRequests.CreateProposalAsync(contextId, threadId,
                        new CreateDraft.Unrank(modal.Reason.Trim()))
                    : Client.Value.MapRequests.ReplaceDraftAsync(contextId, threadId, new MapProposalId(draftId),
                        new CreateDraft.Unrank(modal.Reason.Trim()))
            ).WhenAll()).TryGetValue(out var proposal, out var error))
        {
            await ReportRankingErrorAsync(error);
            return;
        }

        await OpenRankingProposal(contextId, threadId, proposal.Id, listPage, sortBy, origin);
        await FollowupAsync(draftId == 0
                ? "Unranking saved as a private draft. Review the target and submit it when ready."
                : "Replacement unranking draft saved. The previous draft was deleted.",
            ephemeral: true);
    }
}

public class RankingUnrankingModal : IModal
{
    public string Title => "Unranking draft";

    [InputLabel("Unranking reason", "Explain why this ranked entry should be removed. Saves a private draft.")]
    [ModalTextInput("reason", TextInputStyle.Paragraph, minLength: 1, maxLength: 2000)]
    public string Reason { get; set; } = "";
}