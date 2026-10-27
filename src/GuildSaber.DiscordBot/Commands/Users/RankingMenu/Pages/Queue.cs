namespace GuildSaber.DiscordBot.Commands.Users;

/*public partial class UserModuleSlash
{
    [ComponentInteraction("ranking_menu_queue:*,*")]
    public async Task UpdateCurrentMessageToRankingQueuePagedMenu(ContextId contextId, int page)
    {
        var guildId = await GetGuildIdAsync();
        var categories = await Cache.GetGuildCategoriesAsync(guildId, Client.Value);

        var filter = new Filters();
        var sorter = new PaginatedRequestOptions<EMapRequestSorter>
        {
            Page = page,
            PageSize = 3,
            SortBy = EMapRequestSorter.Priority,
            Order = EOrder.Asc
        };

        var submittedThreads = await Client.Value.MapRequests
            .GetQueueAsync(contextId, filter, sorter)
            .Unwrap();

        var component = (await RankingMenuView
                .BuildComponent(guildId, contextId, submittedThreads.Data, categories, EmojiSettings, Client.Value, Cache))
            .WithActionRow(x => x
                .WithButton(label: "Back to menu", customId: $"scoring:{contextId}", style: ButtonStyle.Secondary)
                .WithButton(
                    label: "Prev",
                    disabled: !submittedThreads.HasPreviousPage,
                    customId: $"scoring_admin_conf:{contextId},{submittedThreads.Page - 1}",
                    style: ButtonStyle.Primary)
                .WithButton(
                    label:
                    $"Next ({(submittedThreads.Page - 1) * submittedThreads.PageSize}/{submittedThreads.TotalCount} scores)",
                    disabled: !submittedThreads.HasNextPage,
                    customId: $"scoring_admin_conf:{contextId},{submittedThreads.Page + 1}",
                    style: ButtonStyle.Primary))
            .Build();

        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(x => x.Components = component);
    }
}

file static class RankingMenuView
{
    public static async Task<ComponentBuilderV2> MakeThreadComponent(
        GuildId guildId, MapRequestResponses.Thread thread, CategoryResponses.Category[] categories, GuildSaberClient client, HybridCache cache,
        IOptions<EmojiSettings> emojiSettings)
    {
        var player = await cache.GetPlayerByIdAsync(thread.AuthorId, client);

        return new ComponentBuilderV2()
            .WithContainer(rankedMap.ToContainerBuilder([], categories, emojiSettings))
            .WithSection(rankedScore.ToSection(player, rankedMap, scoreStatistics, rankedScore.EditedAt, emojiSettings))
            .WithActionRow(x => x.WithButton(
                label: "View Replay",
                url: $"https://replay.beatleader.com/?scoreId={blScoreId}",
                disabled: blScoreId is null,
                style: ButtonStyle.Link));
    }

    public static async Task<ComponentBuilderV2> BuildComponent(
        GuildId guildId,
        ContextId contextId,
        MapRequestResponses.Thread[] submittedThreads,
        CategoryResponses.Category[] categories,
        IOptions<EmojiSettings> emojiSettings,
        GuildSaberClient client,
        HybridCache cache)
    {
        var builder = new ComponentBuilderV2();

        if (submittedThreads.Length == 0)
            return builder.WithTextDisplay("No pending scores found.");

        foreach (var data in submittedThreads)
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
}*/