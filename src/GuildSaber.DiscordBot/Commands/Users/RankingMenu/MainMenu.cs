using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.MapRequests.Http;
using GuildSaber.Api.Shared;
using GuildSaber.Common.Helpers;
using GuildSaber.DiscordBot.Core.AutocompleteHandlers;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking:*")]
    [SlashCommand("ranking", "Open the ranking menu")]
    public async Task RankingMenu([Autocomplete<ContextAutocompleteHandler>] ContextId contextId)
    {
        var guild = await (
            Context.Interaction.HasResponded ? Task.CompletedTask : DeferAsync(ephemeral: true),
            GetGuildAsync().AsTask()
        ).WhenAll();

        var (queueResult, allowanceResult, boostsResult) = await (
            Client.Value.MapRequests.GetQueueAsync(contextId, new MapProposalRequests.Filters(),
                new PaginatedRequestOptions<MapProposalRequests.EMapRequestSorter>(Page: 1, PageSize: 1)),
            Client.Value.MapRequests.GetAllowanceAsync(contextId),
            Client.Value.MapRequests.GetBoostsAsync()
        ).WhenAll();

        if (!queueResult.TryGetValue(out var queue, out var error))
        {
            await ReportRankingErrorAsync(error);
            return;
        }

        var allowance = allowanceResult.GetValueOrDefault();
        var boosts = boostsResult.GetValueOrDefault();
        var component = new ComponentBuilderV2()
            .WithContainer(container => container
                .WithAccentColor(Color.Red)
                .WithSection(header => header
                    .WithTextDisplay("### Welcome to the Ranking Menu!")
                    .WithAccessory(new ThumbnailBuilder()
                        .WithMedia(Client.Value.Guilds.GetLogoUrl(guild.Id).ToString())))
                .WithSeparator()
                .WithSection(content => content.WithTextDisplay(
                        queue.TotalCount == 0
                            ? $"**No threads** in the queue today! {EmojiSettings.Value.Congrats}"
                            : $"There are **{queue.TotalCount} threads** in the queue today {EmojiSettings.Value.NeedConfirmation}")
                    .WithAccessory(new ButtonBuilder()
                        .WithLabel("Check the queue")
                        .WithCustomId(
                            $"ranking_menu_queue:{contextId},1,{(int)MapProposalRequests.EMapRequestSorter.Priority},{(int)ERankingThreadOrigin.Queue}")
                        .WithStyle(ButtonStyle.Success)
                        .WithDisabled(queue.TotalCount == 0)))
                .WithTextDisplay(allowance is null || boosts is null
                    ? "Your personal requests could not be loaded. Link your Discord account on GuildSaber or refresh to try again."
                    : $"**{allowance.Drafts}/{allowance.MaxDrafts}** private drafts in this context · " +
                      $"**{boosts.Allocations.Length}/{boosts.Limit}** boosts used across GuildSaber.")
                .WithActionRow(row => row
                    .WithButton("New request", $"ranking_new:{contextId}", ButtonStyle.Success,
                        disabled: allowance is null || allowance.Drafts >= allowance.MaxDrafts)
                    .WithButton(allowance is null ? "My drafts" : $"My drafts ({allowance.Drafts})",
                        $"ranking_menu_drafts:{contextId},1", disabled: allowance is null)
                    .WithButton(
                        boosts is null
                            ? "My boosted threads"
                            : $"My boosted threads ({boosts.Allocations.Count(x => x.ContextId == contextId)})",
                        $"ranking_menu_queue:{contextId},1,{(int)MapProposalRequests.EMapRequestSorter.Priority},{(int)ERankingThreadOrigin.Boosts}",
                        ButtonStyle.Secondary, disabled: boosts is null))
                .WithTextDisplay(
                    "Start a new request with a BeatSaver link, key or hash. Existing threads are shown before creating a draft. " +
                    "Use /search to find a ranked map, open its thread and fork the current accepted proposal. " +
                    "You can also fork an open proposal from the queue. Forks stay private until submitted."))
            .Build();

        await ModifyOriginalResponseAsync(x =>
        {
            x.Flags = MessageFlags.ComponentsV2;
            x.Components = component;
            x.AllowedMentions = AllowedMentions.None;
        });
    }
}