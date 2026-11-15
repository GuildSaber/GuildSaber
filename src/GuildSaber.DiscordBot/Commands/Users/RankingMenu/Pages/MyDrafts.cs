using Discord;
using Discord.Interactions;
using GuildSaber.Api.Shared;
using GuildSaber.Common.Helpers;
using static GuildSaber.Api.Features.MapRequests.Http.MapProposalRequests;
using static GuildSaber.Api.Features.MapRequests.Http.MapRequestResponses;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking_menu_drafts:*,*")]
    public async Task ShowMyRankingDraftsAsync(ContextId contextId, int page)
    {
        if (!(await (
                DeferRankingInteractionAsync(),
                Client.Value.MapRequests.GetMineAsync(contextId, new Filters(IsDraft: true),
                    new PaginatedRequestOptions<EMapProposalSorter>(Page: Math.Max(1, page), PageSize: 3,
                        SortBy: EMapProposalSorter.CreatedAt, Order: EOrder.Desc))
            ).WhenAll())
            .TryGetValue(out var drafts, out var error))
        {
            await ReportRankingErrorAsync(error);
            return;
        }

        if (drafts.Page > Math.Max(1, drafts.TotalPages))
        {
            await ShowMyRankingDraftsAsync(contextId, Math.Max(1, drafts.TotalPages));
            return;
        }

        var component = RankingDraftListView.BuildComponent(contextId, drafts).Build();
        await ModifyOriginalResponseAsync(x =>
        {
            x.Flags = MessageFlags.ComponentsV2;
            x.Components = component;
            x.AllowedMentions = AllowedMentions.None;
        });
    }
}

file static class RankingDraftListView
{
    public static ComponentBuilderV2 BuildComponent(ContextId contextId, PagedList<Proposal> drafts)
    {
        var builder = new ComponentBuilderV2().WithTextDisplay("### My private drafts\n" +
                                                               "Only you can see these. Open a draft to revise it, submit it for review or delete it.");

        foreach (var draft in drafts.Data)
        {
            var definition = draft.GetPreviewDefinition();
            var version = definition.MapVersions[0];
            builder.WithContainer(container => container.WithAccentColor(Color.Orange)
                .WithSection(section => section
                    .WithTextDisplay($"**{Format.Sanitize(version.Song.Info.BeatSaverName)}**\n" +
                                     $"{version.Difficulty.Difficulty}, {version.Difficulty.GameMode} · {version.PlayModeName}\n" +
                                     (draft.Change is ProposalChange.Unrank
                                         ? "**Unranking draft**"
                                         : $"⭐: {definition.Rating.DiffStar?.ToString("0.00") ?? "Auto"} | " +
                                           $"✨: {definition.Rating.AccStar?.ToString("0.00") ?? "Auto"}"))
                    .WithAccessory(
                        new ThumbnailBuilder().WithMedia($"https://cfcdn.beatsaver.com/{version.Song.Hash}.jpg")))
                .WithSection(section => section
                    .WithTextDisplay($"Draft **#{draft.Id}** · Thread **#{draft.ThreadId}** · " +
                                     $"**{definition.MapVersions.Length}** {(definition.MapVersions.Length == 1 ? "map version" : "map versions")}\n" +
                                     TimestampTag.FormatFromDateTimeOffset(draft.CreatedAt,
                                         TimestampTagStyles.Relative))
                    .WithAccessory(new ButtonBuilder().WithLabel("Open draft")
                        .WithCustomId(
                            $"ranking_pick:{contextId},{draft.ThreadId},{draft.Id},{drafts.Page},{(int)EMapRequestSorter.Priority},{(int)UserModuleSlash.ERankingThreadOrigin.Drafts}")
                        .WithStyle(ButtonStyle.Primary))));
        }

        if (drafts.Data.Length == 0)
            builder.WithTextDisplay("You have no private drafts in this context. Fork a proposal from the queue, " +
                                    "or use /search to fork a ranked map's current accepted proposal.");

        return builder
            .WithTextDisplay(
                $"Page: **{drafts.Page}**/{Math.Max(1, drafts.TotalPages)} (**{drafts.TotalCount}** drafts)")
            .WithActionRow(row => row
                .WithButton("Back to menu", $"ranking:{contextId}", ButtonStyle.Secondary)
                .WithButton("Prev", $"ranking_menu_drafts:{contextId},{drafts.Page - 1}",
                    disabled: !drafts.HasPreviousPage)
                .WithButton("Next", $"ranking_menu_drafts:{contextId},{drafts.Page + 1}",
                    disabled: !drafts.HasNextPage)
                .WithButton("Refresh", $"ranking_menu_drafts:{contextId},{drafts.Page}", ButtonStyle.Secondary));
    }
}