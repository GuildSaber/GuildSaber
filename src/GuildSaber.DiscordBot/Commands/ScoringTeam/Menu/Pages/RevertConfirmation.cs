using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.Api.Features.Guilds.Categories.Http;
using GuildSaber.Api.Features.RankedScores.Http;
using GuildSaber.Api.Features.Scores.Http;
using GuildSaber.Api.Shared;
using GuildSaber.Common.Helpers;
using GuildSaber.Common.Result;
using GuildSaber.CSharpClient;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Settings;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using static GuildSaber.Api.Features.RankedScores.Http.RankedScoreRequests;

namespace GuildSaber.DiscordBot.Commands.ScoringTeam;

public partial class ScoringTeamModuleSlash
{
    [ComponentInteraction("scoring_admin_conf_reviewed:*,*")]
    public async Task UpdateCurrentMessageToReviewedAdminConfirmationPagedMenu(ContextId contextId, int page)
    {
        var guildId = await GetGuildIdAsync();
        var categories = await Cache.GetGuildCategoriesAsync(guildId, Client.Value);

        var filter = new Filters(RankedScoreTypes: ERankedScoreType.Accepted | ERankedScoreType.Refused);
        var sorter = new PaginatedRequestOptions<ERankedScoreSorter>
        {
            Page = page,
            PageSize = 3,
            SortBy = ERankedScoreSorter.EditTime,
            Order = EOrder.Desc
        };

        var pendingScores = await Client.Value.RankedScores
            .GetAsync(contextId, filter, sorter)
            .Unwrap();

        var component = (await ScoringTeamMenuView
                .BuildComponent(contextId, pendingScores.Data, categories, EmojiSettings, Client.Value, Cache))
            .WithActionRow(x => x
                .WithButton(label: "Back to menu", customId: $"scoring:{contextId}", style: ButtonStyle.Secondary)
                .WithButton(
                    label: "Prev",
                    disabled: !pendingScores.HasPreviousPage,
                    customId: $"scoring_admin_conf_reviewed:{contextId},{pendingScores.Page - 1}",
                    style: ButtonStyle.Primary)
                .WithButton(
                    label:
                    $"Next ({(pendingScores.Page - 1) * pendingScores.PageSize}/{pendingScores.TotalCount} scores)",
                    disabled: !pendingScores.HasNextPage,
                    customId: $"scoring_admin_conf_reviewed:{contextId},{pendingScores.Page + 1}",
                    style: ButtonStyle.Primary))
            .Build();

        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(x => x.Components = component);
    }

    [ComponentInteraction("scoring-team-menu:score:revert:*,*")]
    public async Task RevertScore(ContextId contextId, ScoreId id)
    {
        var rankedScores = await Client.Value.RankedScores.RevertToPendingAsync(contextId, id).Unwrap();

        if (rankedScores is null)
        {
            await UpdateCurrentMessageToReviewedAdminConfirmationPagedMenu(contextId, page: 1);
            await Context.Interaction.FollowupAsync(":x: Data was stale (or already handled), refreshing to page 1.");
            return;
        }

        await UpdateCurrentMessageToReviewedAdminConfirmationPagedMenu(contextId, page: 1);
        await Context.Interaction.FollowupAsync(":white_check_mark: Score reverted to pending.");
    }
}

file static class ScoringTeamMenuView
{
    public static async Task<ComponentBuilderV2> BuildComponent(
        ContextId contextId,
        RankedScoreResponses.RankedScoreWithRankedMap[] pendingScores,
        CategoryResponses.Category[] categories,
        IOptions<EmojiSettings> emojiSettings,
        GuildSaberClient client,
        HybridCache cache)
    {
        var builder = new ComponentBuilderV2();

        if (pendingScores.Length == 0)
            return builder.WithTextDisplay("No pending scores found.");

        foreach (var data in pendingScores)
        {
            var (player, scoreStatistics) = await (
                cache.GetPlayerByIdAsync(data.RankedScore.PlayerId, client).AsTask(),
                client.Scores.GetStatisticsAsync(data.RankedScore.Score.Id).Unwrap()
            ).WhenAll();

            if (player is null)
                throw new InvalidOperationException("Player not found.");

            var blScoreId = (data.RankedScore.Score as ScoreResponses.Score.BeatLeaderScore)?.BeatLeaderScoreId;
            builder.WithContainer(data.RankedMap.ToContainerBuilder([], categories, emojiSettings)
                .WithSeparator()
                .WithSection(data.RankedScore.ToSection(player, data.RankedMap, scoreStatistics,
                    data.RankedScore.EditedAt, emojiSettings))
                .WithActionRow(y => y
                    .WithButton(
                        label: "Revert to pending",
                        customId: $"scoring-team-menu:score:revert:{contextId},{data.RankedScore.Score.Id}",
                        style: ButtonStyle.Danger)
                    .WithButton(
                        label: "View Replay",
                        url: $"https://replay.beatleader.com/?scoreId={blScoreId}",
                        disabled: blScoreId is null,
                        style: ButtonStyle.Link)));
        }

        return builder;
    }
}