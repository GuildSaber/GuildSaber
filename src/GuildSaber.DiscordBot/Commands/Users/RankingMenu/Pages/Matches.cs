using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Shared;
using GuildSaber.Common.Helpers;
using GuildSaber.DiscordBot.Core.Extensions;
using RankDraft = GuildSaber.Api.Features.MapRequests.Http.MapProposalRequests.CreateDraft.Rank;
using static GuildSaber.Api.Features.MapRequests.Http.MapProposalRequests;
using static GuildSaber.Api.Features.MapRequests.Http.MapRequestResponses;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking_new_matches:*,*")]
    public Task ShowNewRankingMatchesAsync(string sessionId, int page)
        => ShowNewRankingMatchesAsync(sessionId, page, prepareIfEmpty: false);

    [ComponentInteraction("ranking_new_check:*")]
    public Task CheckNewRankingMatchesAsync(string sessionId)
        => ShowNewRankingMatchesAsync(sessionId, 1, prepareIfEmpty: true);

    private async Task ShowNewRankingMatchesAsync(string sessionId, int page, bool prepareIfEmpty)
    {
        if (await GetRankingMapSelectionAsync(sessionId) is not { } selection) return;

        var difficulty = selection.Lookup.Difficulties[selection.DifficultyIndex];
        if (!(await (
                DeferRankingInteractionAsync(),
                Client.Value.MapRequests.GetMatchesAsync(selection.ContextId,
                    new MatchFilters(selection.Lookup.Song.Hash, difficulty.Characteristic, difficulty.Difficulty),
                    new PaginatedRequestOptions<EMapThreadSorter>(Page: Math.Max(1, page), PageSize: 3))
            ).WhenAll())
            .TryGetValue(out var matches, out var error))
        {
            await ReportRankingErrorAsync(error);
            return;
        }

        if (matches.Page > Math.Max(1, matches.TotalPages))
        {
            await ShowNewRankingMatchesAsync(sessionId, Math.Max(1, matches.TotalPages));
            return;
        }

        if (prepareIfEmpty && matches.TotalCount == 0)
        {
            await StartNewRankingDraftAsync(sessionId);
            await FollowupAsync("No matching threads are visible in this context. Your private draft editor is ready.",
                ephemeral: true);
            return;
        }

        await ModifyOriginalResponseAsync(x =>
        {
            x.Components = RankingMapMatchesView.BuildComponent(sessionId, selection, matches).Build();
            x.AllowedMentions = AllowedMentions.None;
        });
    }

    [ComponentInteraction("ranking_match_open:*,*,*")]
    public Task OpenMatchingRankingThreadAsync(ContextId contextId, MapRequestThreadId threadId, long proposalId)
        => ShowRankingThreadAsync(contextId, threadId,
            new RankingThreadNavigation(ERankingThreadTab.Current, 1, 1, 1, EMapRequestSorter.Priority,
                ERankingThreadOrigin.Matches), proposalId == 0 ? null : new MapProposalId(proposalId),
            newMessage: true);

    [ComponentInteraction("ranking_new_draft:*")]
    public async Task StartNewRankingDraftAsync(string sessionId)
    {
        if (await GetRankingMapSelectionAsync(sessionId, defer: true) is not { } selection) return;

        if (selection.EditorId is { } previousEditorId &&
            await Cache.GetOrCreateAsync<RankingDraftEditor?>($"RankingDraftEditor_{previousEditorId}",
                static _ => ValueTask.FromResult<RankingDraftEditor?>(null)) is { } editor
            && editor.OwnerId == Context.User.DiscordId)
        {
            await ShowDraftEditorAsync(previousEditorId);
            return;
        }

        var difficulty = selection.Lookup.Difficulties[selection.DifficultyIndex];
        var editorId = Guid.NewGuid().ToString("N");
        await StoreDraftEditorAsync(editorId, new RankingDraftEditor(Context.User.DiscordId, selection.ContextId,
            new RankingDraftTarget.NewThread(sessionId),
            new RankDraft([
                    new MapVersionInput(new SongReference.ByHash(selection.Lookup.Song.Hash),
                        difficulty.Characteristic, difficulty.Difficulty,
                        selection.Lookup.PlayModes[selection.PlayModeIndex], 0)
                ],
                new RankedMapRequests.RankedMapRequirements(), new RankedMapRequests.ManualRating(), [], []),
            1, EMapRequestSorter.Priority, ERankingThreadOrigin.Drafts));
        await StoreRankingMapSelectionAsync(sessionId, selection with { EditorId = editorId });
        await ShowDraftEditorAsync(editorId);
    }
}

file static class RankingMapMatchesView
{
    public static ComponentBuilderV2 BuildComponent(
        string sessionId, RankingMapSelection selection, PagedList<ThreadMatch> matches)
    {
        var difficulty = selection.Lookup.Difficulties[selection.DifficultyIndex];
        var builder = new ComponentBuilderV2().WithContainer(container => container
            .WithAccentColor(matches.TotalCount == 0 ? Color.Green : Color.Orange)
            .WithTextDisplay(
                $"### Matching threads\n**[{Format.Sanitize(selection.Lookup.Song.Info.BeatSaverName)}](https://beatsaver.com/maps/{selection.Lookup.Song.Key})** ({selection.Lookup.Song.Key.ToBsrKey()})\n" +
                $"{difficulty.Difficulty}, {difficulty.Characteristic} · {selection.Lookup.PlayModes[selection.PlayModeIndex]}")
            .WithTextDisplay(matches.TotalCount == 0
                ? "No matching threads in this context."
                : $"**{matches.TotalCount}** matching {(matches.TotalCount == 1 ? "thread" : "threads")} across all play modes.\n" +
                  "Use an existing thread to suggest changes, or create a separate ranking request."));

        foreach (var match in matches.Data)
        {
            var proposalId = match switch
            {
                ThreadMatch.Ranked => (MapProposalId?)null,
                ThreadMatch.Open(_, var id) => id,
                ThreadMatch.Draft(_, var id) => id,
                ThreadMatch.History(_, var id) => id
            };
            builder.WithContainer(container => container
                .WithAccentColor(match is ThreadMatch.Ranked ? Color.Green : Color.Orange)
                .WithSection(section => section.WithTextDisplay(match switch
                    {
                        ThreadMatch.Ranked(_, var rankedMapId) =>
                            $"**Ranked map #{rankedMapId}** · Thread #{match.ThreadId}",
                        ThreadMatch.Open =>
                            $"**Open proposal #{proposalId}** · Thread #{match.ThreadId}",
                        ThreadMatch.Draft =>
                            $"**Your private draft #{proposalId}** · Thread #{match.ThreadId}",
                        ThreadMatch.History =>
                            $"**Previous proposal #{proposalId}** · Thread #{match.ThreadId}"
                    })
                    .WithAccessory(new ButtonBuilder()
                        .WithLabel(match is ThreadMatch.Draft ? "Open draft" : "Open thread")
                        .WithCustomId(
                            $"ranking_match_open:{selection.ContextId},{match.ThreadId},{proposalId?.Value ?? 0}")
                        .WithStyle(ButtonStyle.Primary))));
        }

        return builder
            .WithTextDisplay($"Page: **{matches.Page}**/{Math.Max(1, matches.TotalPages)}")
            .WithActionRow(row => row
                .WithButton("Back to map selection",
                    $"ranking_new_map:{sessionId},{selection.DifficultyIndex / 25 + 1},{selection.PlayModeIndex / 25 + 1}",
                    ButtonStyle.Secondary)
                .WithButton("Prev", $"ranking_new_matches:{sessionId},{matches.Page - 1}",
                    disabled: !matches.HasPreviousPage)
                .WithButton("Next", $"ranking_new_matches:{sessionId},{matches.Page + 1}",
                    disabled: !matches.HasNextPage))
            .WithActionRow(row => row
                .WithButton(selection.EditorId is not null ? "Continue draft" :
                    matches.TotalCount == 0                ? "Prepare private draft" : "Prepare a separate draft",
                    $"ranking_new_draft:{sessionId}",
                    matches.TotalCount == 0 ? ButtonStyle.Success : ButtonStyle.Secondary)
                .WithButton("Cancel", $"ranking:{selection.ContextId}", ButtonStyle.Secondary));
    }
}