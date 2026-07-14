using CSharpFunctionalExtensions;
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

namespace GuildSaber.DiscordBot.Commands.ScoringTeam;

public partial class ScoringTeamModuleSlash
{
    public enum AcceptOrRefuse { Accept, Refuse }

    [ComponentInteraction("scoring_admin_conf:*,*")]
    public async Task UpdateCurrentMessageToAdminConfirmationPagedMenu(ContextId contextId, int page)
    {
        var guildId = await GetGuildIdAsync();
        var categories = await Cache.GetGuildCategoriesAsync(guildId, Client.Value);

        var filter = new RankedScoreRequests.Filters(RankedScoreTypes: RankedScoreRequests.ERankedScoreType.Pending);
        var sorter = new PaginatedRequestOptions<RankedScoreRequests.ERankedScoreSorter>
        {
            Page = page,
            PageSize = 3,
            SortBy = RankedScoreRequests.ERankedScoreSorter.ScoreTime,
            Order = EOrder.Asc
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
                    customId: $"scoring_admin_conf:{contextId},{pendingScores.Page - 1}",
                    style: ButtonStyle.Primary)
                .WithButton(
                    label:
                    $"Next ({(pendingScores.Page - 1) * pendingScores.PageSize}/{pendingScores.TotalCount} scores)",
                    disabled: !pendingScores.HasNextPage,
                    customId: $"scoring_admin_conf:{contextId},{pendingScores.Page + 1}",
                    style: ButtonStyle.Primary))
            .Build();

        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(x => x.Components = component);
    }

    [ComponentInteraction("scoring-team-menu:score:accept:*,*")]
    public Task AcceptScore(ContextId contextId, ScoreId id) => AcceptOrDenyScore(AcceptOrRefuse.Accept, contextId, id);

    [ComponentInteraction("scoring-team-menu:score:refuse:*,*")]
    public Task RefuseScore(ContextId contextId, ScoreId id) => AcceptOrDenyScore(AcceptOrRefuse.Refuse, contextId, id);

    public async Task AcceptOrDenyScore(AcceptOrRefuse action, ContextId contextId, ScoreId id)
    {
        var (guild, rankedScores) = await (
            GetGuildAsync().AsTask(),
            action is AcceptOrRefuse.Accept
                ? Client.Value.RankedScores.SetConfirmedAsync(contextId, id).Unwrap()
                : Client.Value.RankedScores.SetRefusedAsync(contextId, id).Unwrap()
        ).WhenAll();

        if (rankedScores is null)
        {
            await UpdateCurrentMessageToAdminConfirmationPagedMenu(contextId, page: 1);
            await Context.Interaction.FollowupAsync(":x: Data was stale (or already handled), refreshing to page 1.");
            return;
        }

        IMessageChannel? feedChannel = null;
        var feedChannelId = action is AcceptOrRefuse.Accept
            ? guild.DiscordInfo.ConfirmedScoreFeedChannelId
            : guild.DiscordInfo.RefusedScoreFeedChannelId;

        if (feedChannelId is not null)
        {
            var channel = await Context.Client.GetChannelAsync(feedChannelId.Value);
            if (channel is null)
                await Context.Interaction.FollowupAsync(":x: Channel not found for ConfirmedScoreFeedChannelId.");

            if (channel is IMessageChannel messageChannel)
                feedChannel = messageChannel;
            else await Context.Interaction.FollowupAsync(":x: Invalid channel type for ConfirmedScoreFeedChannelId.");
        }

        await UpdateCurrentMessageToAdminConfirmationPagedMenu(contextId, page: 1);
        feedChannel ??= (IMessageChannel)await Context.Interaction.GetChannelAsync();

        IUserMessage? firstFeedMessage = null;
        foreach (var group in rankedScores.GroupBy(x => x.RankedMapId))
        {
            // Only display the first ranked score for a map because we don't yet show all scores per points.
            var first = group.First();

            var groupMessage = await feedChannel
                .SendMessageAsync(components: (await ScoringTeamMenuView
                    .MakeScoresComponent(first, Client.Value, Cache, EmojiSettings)).Build());

            firstFeedMessage ??= groupMessage;
        }

        if (firstFeedMessage != null)
            await Context.Interaction.FollowupAsync(
                $"{(action == AcceptOrRefuse.Accept ? EmojiSettings.Value.Confirmed : EmojiSettings.Value.Refused)} {firstFeedMessage.Link}",
                ephemeral: true);
    }
}

file static class ScoringTeamMenuView
{
    public static async Task<ComponentBuilderV2> MakeScoresComponent(
        RankedScoreResponses.RankedScore rankedScore, GuildSaberClient client, HybridCache cache,
        IOptions<EmojiSettings> emojiSettings)
    {
        var (player, rankedMap, scoreStatistics) = await (
            cache.GetPlayerByIdAsync(rankedScore.PlayerId, client).AsTask(),
            client.RankedMaps.GetByIdAsync(rankedScore.RankedMapId).EnsureNotNull("Map not found").Unwrap(),
            client.Scores.GetStatisticsAsync(rankedScore.Score.Id).Unwrap()
        ).WhenAll();

        if (player is null)
            throw new InvalidOperationException("Player not found.");

        var categories = await cache.GetGuildCategoriesAsync(rankedMap.GuildId, client);
        var blScoreId = (rankedScore.Score as ScoreResponses.Score.BeatLeaderScore)?.BeatLeaderScoreId;

        return new ComponentBuilderV2()
            .WithContainer(rankedMap.ToContainerBuilder([], categories, emojiSettings))
            .WithSection(rankedScore.ToSection(player, rankedMap, scoreStatistics, emojiSettings))
            .WithActionRow(x => x.WithButton(
                label: "View Replay",
                url: $"https://replay.beatleader.com/?scoreId={blScoreId}",
                disabled: blScoreId is null,
                style: ButtonStyle.Link));
    }

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
                .WithSection(data.RankedScore.ToSection(player, data.RankedMap, scoreStatistics, emojiSettings))
                .WithActionRow(y => y
                    .WithButton(
                        label: "Accept",
                        customId: $"scoring-team-menu:score:accept:{contextId},{data.RankedScore.Score.Id}",
                        style: ButtonStyle.Success)
                    .WithButton(
                        label: "Refuse",
                        customId: $"scoring-team-menu:score:refuse:{contextId},{data.RankedScore.Score.Id}",
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