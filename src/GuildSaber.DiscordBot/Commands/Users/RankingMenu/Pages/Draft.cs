using System.Text.Json.Serialization;
using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Categories.Http;
using GuildSaber.Common.Helpers;
using GuildSaber.DiscordBot.Core.Extensions;
using Microsoft.Extensions.Caching.Hybrid;
using RankDraft = GuildSaber.Api.Features.MapRequests.Http.MapProposalRequests.CreateDraft.Rank;
using static GuildSaber.Api.Features.MapRequests.Http.MapProposalRequests;
using static GuildSaber.Api.Features.MapRequests.Http.MapRequestResponses;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking_draft:*,*,*,*,*,*")]
    public Task ReviseRankingDraft(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, int listPage,
        EMapRequestSorter sortBy, ERankingThreadOrigin origin)
        => StartRankingDraftAsync(contextId, threadId, proposalId, listPage, sortBy, origin, isFork: false);

    [ComponentInteraction("ranking_fork:*,*,*,*,*,*")]
    public Task ForkRankingProposal(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, int listPage,
        EMapRequestSorter sortBy, ERankingThreadOrigin origin)
        => StartRankingDraftAsync(contextId, threadId, proposalId, listPage, sortBy, origin, isFork: true);

    private async Task StartRankingDraftAsync(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, int listPage,
        EMapRequestSorter sortBy, ERankingThreadOrigin origin, bool isFork)
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

        if (result.Value is not { } proposal || !isFork && proposal is not Proposal.Draft)
        {
            await OpenRankingProposal(contextId, threadId, proposalId, listPage, sortBy, origin);
            await FollowupAsync(
                "This proposal is no longer available for this action. Its current state has been loaded.",
                ephemeral: true);
            return;
        }

        var sessionId = Guid.NewGuid().ToString("N");
        await StoreDraftEditorAsync(sessionId, new RankingDraftEditor(Context.User.DiscordId, contextId,
            isFork
                ? new RankingDraftTarget.Fork(threadId, proposalId)
                : new RankingDraftTarget.Revision(threadId, proposalId),
            proposal.GetPreviewDefinition().ToDraft(proposal.Change is ProposalChange.Rank ? proposal.Comment : null),
            listPage, sortBy, origin));
        await ShowDraftEditorAsync(sessionId);
    }

    [ComponentInteraction("ranking_draft_edit:*")]
    public async Task ShowDraftEditorAsync(string sessionId)
    {
        if (await GetDraftEditorAsync(sessionId) is not { } revision) return;

        var categories = await (
            DeferRankingInteractionAsync(),
            Cache.GetGuildCategoriesAsync(await GetGuildIdAsync(), Client.Value).AsTask()
        ).WhenAll();
        await ModifyOriginalResponseAsync(x =>
        {
            x.Components = RankingDraftView.BuildComponent(sessionId, revision, categories).Build();
            x.AllowedMentions = AllowedMentions.None;
        });
    }

    [ComponentInteraction("ranking_draft_save:*")]
    public async Task SaveDraftEditorAsync(string sessionId)
    {
        if (await GetDraftEditorAsync(sessionId) is not { } revision) return;

        if (!(await (
                DeferRankingInteractionAsync(),
                revision.Target switch
                {
                    RankingDraftTarget.NewThread => Client.Value.MapRequests.CreateAsync(revision.ContextId,
                        revision.Definition),
                    RankingDraftTarget.Fork(var threadId, _) => Client.Value.MapRequests.CreateProposalAsync(
                        revision.ContextId, threadId, revision.Definition),
                    RankingDraftTarget.Revision(var threadId, var proposalId) => Client.Value.MapRequests
                        .ReplaceDraftAsync(
                            revision.ContextId, threadId, proposalId, revision.Definition)
                }
            ).WhenAll()).TryGetValue(out var proposal, out var error))
        {
            await ReportRankingErrorAsync(error);
            return;
        }

        await Cache.RemoveAsync($"RankingDraftEditor_{sessionId}");
        if (revision.Target is RankingDraftTarget.NewThread(var selectionId))
            await Cache.RemoveAsync($"RankingMapSelection_{selectionId}");
        await OpenRankingProposal(revision.ContextId, proposal.ThreadId, proposal.Id, revision.ListPage,
            revision.SortBy, revision.Origin);
        await FollowupAsync(revision.Target switch
            {
                RankingDraftTarget.NewThread =>
                    $"Saved private draft **#{proposal.Id}** in new thread **#{proposal.ThreadId}**. Submit it when you are ready for review.",
                RankingDraftTarget.Fork(_, var originalId) =>
                    $"Saved private draft **#{proposal.Id}** from proposal **#{originalId}**. Submit it when you are ready for review.",
                RankingDraftTarget.Revision(_, var originalId) =>
                    $"Saved private draft **#{proposal.Id}**. Previous draft **#{originalId}** was deleted."
            },
            ephemeral: true);
    }

    [ComponentInteraction("ranking_draft_cancel:*")]
    public async Task CancelDraftEditorAsync(string sessionId)
    {
        if (await GetDraftEditorAsync(sessionId, defer: true) is not { } revision) return;

        if (revision.Target is not RankingDraftTarget.NewThread)
            await Cache.RemoveAsync($"RankingDraftEditor_{sessionId}");
        await (revision.Target switch
        {
            RankingDraftTarget.NewThread(var selectionId) => ShowNewRankingMatchesAsync(selectionId, 1),
            RankingDraftTarget.Fork(var threadId, var proposalId) => OpenRankingProposal(revision.ContextId,
                threadId, proposalId, revision.ListPage, revision.SortBy, revision.Origin),
            RankingDraftTarget.Revision(var threadId, var proposalId) => OpenRankingProposal(revision.ContextId,
                threadId, proposalId, revision.ListPage, revision.SortBy, revision.Origin)
        });
    }

    private async ValueTask<RankingDraftEditor?> GetDraftEditorAsync(string sessionId, bool defer = false)
    {
        var revision = await (
            defer ? DeferRankingInteractionAsync() : Task.CompletedTask,
            Cache.GetOrCreateAsync<RankingDraftEditor?>($"RankingDraftEditor_{sessionId}",
                static _ => ValueTask.FromResult<RankingDraftEditor?>(null)).AsTask()
        ).WhenAll();

        if (revision is not null && revision.OwnerId == Context.User.DiscordId) return revision;

        const string message =
            "This draft editor has expired or belongs to another player. Open /ranking to find your saved drafts or fork a proposal again.";
        await (Context.Interaction.HasResponded
            ? FollowupAsync(message, ephemeral: true)
            : RespondAsync(message, ephemeral: true));
        return null;
    }

    private ValueTask StoreDraftEditorAsync(string sessionId, RankingDraftEditor revision)
        => Cache.SetAsync($"RankingDraftEditor_{sessionId}", revision,
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(20), LocalCacheExpiration = TimeSpan.FromMinutes(20)
            });
}

public sealed record RankingDraftEditor(
    DiscordId OwnerId,
    ContextId ContextId,
    RankingDraftTarget Target,
    RankDraft Definition,
    int ListPage,
    EMapRequestSorter SortBy,
    UserModuleSlash.ERankingThreadOrigin Origin = UserModuleSlash.ERankingThreadOrigin.Queue)
{
    [JsonIgnore] public bool IsRevision => Target is RankingDraftTarget.Revision;
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(NewThread), "NewThread")]
[JsonDerivedType(typeof(Fork), "Fork")]
[JsonDerivedType(typeof(Revision), "Revision")]
public closed record RankingDraftTarget
{
public sealed record NewThread(string SelectionId) : RankingDraftTarget;
public sealed record Fork(MapRequestThreadId ThreadId, MapProposalId ProposalId) : RankingDraftTarget;
public sealed record Revision(MapRequestThreadId ThreadId, MapProposalId ProposalId) : RankingDraftTarget;
}

file static class RankingDraftView
{
    public static ComponentBuilderV2 BuildComponent(
        string sessionId, RankingDraftEditor revision, CategoryResponses.Category[] categories)
        => new ComponentBuilderV2()
            .WithContainer(container => container.WithAccentColor(Color.Orange)
                .WithTextDisplay(revision.Target switch
                {
                    RankingDraftTarget.NewThread => "### New ranking request\n" +
                                                    "Adjust the definition, then save a private draft in a new thread. Submit it separately when ready for review. " +
                                                    "Saving also verifies leaderboard availability. Unsaved changes expire after 20 minutes.",
                    RankingDraftTarget.Fork(_, var id) => $"### Fork proposal #{id} as a private draft\n" +
                                                          "Adjust the copied definition, then save your own draft in this thread. Submit it separately when ready for review. " +
                                                          "Unsaved changes expire after 20 minutes.",
                    RankingDraftTarget.Revision(_, var id) => $"### Revise private draft #{id}\n" +
                                                              "Changes stay in this editor until you save. Saving creates a new private draft and deletes the previous one. " +
                                                              "Cancel keeps the original draft. Unsaved changes expire after 20 minutes."
                })
                .WithSeparator()
                .WithTextDisplay(
                    $"**Difficulty stars:** {revision.Definition.Rating.DifficultyStar?.ToString("0.00") ?? "Auto on submission"}\n" +
                    $"**Accuracy stars:** {revision.Definition.Rating.AccuracyStar?.ToString("0.00") ?? "Auto on submission"}\n" +
                    $"**Score requirements:** {revision.Definition.Requirements.ToDisplayText()}\n" +
                    $"**Categories:** {FormatCategories(revision.Definition, categories)}\n" +
                    $"**Map-list associations:** {revision.Definition.AchievementIds.Length}\n" +
                    $"**Map versions:** {revision.Definition.MapVersions.Length}")
                .WithTextDisplay(string.IsNullOrWhiteSpace(revision.Definition.Comment)
                    ? "**Author's note:** None"
                    : $"**Author's note**\n{revision.Definition.Comment}")
                .WithActionRow(row => row
                    .WithButton("Ratings", $"ranking_draft_ratings:{sessionId}", ButtonStyle.Secondary)
                    .WithButton("Requirements", $"ranking_draft_requirements:{sessionId}", ButtonStyle.Secondary)
                    .WithButton("Categories", $"ranking_draft_options:{sessionId},Categories,1", ButtonStyle.Secondary)
                    .WithButton("Map lists", $"ranking_draft_options:{sessionId},Achievements,1", ButtonStyle.Secondary)
                    .WithButton("Author's note", $"ranking_draft_note:{sessionId}", ButtonStyle.Secondary))
                .WithActionRow(row => row
                    .WithButton("Map versions", $"ranking_draft_versions:{sessionId},1", ButtonStyle.Secondary)
                    .WithButton(revision.IsRevision ? "Save revised draft" : "Save private draft",
                        $"ranking_draft_save:{sessionId}", ButtonStyle.Success)
                    .WithButton(revision.Target switch
                        {
                            RankingDraftTarget.NewThread => "Back to map matches",
                            RankingDraftTarget.Fork => "Cancel fork",
                            _ => "Cancel changes"
                        },
                        $"ranking_draft_cancel:{sessionId}", ButtonStyle.Secondary)));

    private static string FormatCategories(RankDraft definition, CategoryResponses.Category[] categories)
        => definition.CategoryIds.Length == 0
            ? "None"
            : string.Join(", ", definition.CategoryIds.Take(8)
                  .Select(id => categories.Where(x => x.Id == id).Select(x => Format.Sanitize(x.Info.Name))
                      .DefaultIfEmpty("Deleted category").First()))
              + (definition.CategoryIds.Length > 8 ? $" (+{definition.CategoryIds.Length - 8} more)" : "");
}