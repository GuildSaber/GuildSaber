using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.Api.Features.MapRequests.Http;
using GuildSaber.Api.Shared;
using GuildSaber.Common.Result;
using GuildSaber.DiscordBot.Core.AutocompleteHandlers;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking:*")]
    [SlashCommand("ranking", "Open the ranking menu")]
    public async Task RankingMenu([Autocomplete<ContextAutocompleteHandler>] ContextId contextId)
    {
        var guild = await GetGuildAsync();
        var queueCount = (await Client.Value.MapRequests.GetQueueAsync(contextId, new MapProposalRequests.Filters(),
                new PaginatedRequestOptions<MapProposalRequests.EMapRequestSorter>(Page: 1, PageSize: 1)))
            .Unwrap()
            .TotalCount;

        var component = new ComponentBuilderV2()
            .WithContainer(container => container
                .WithAccentColor(Color.Red)
                .WithSection(header => header
                    .WithTextDisplay("### Welcome to the Ranking Menu!")
                    .WithAccessory(new ThumbnailBuilder()
                        .WithMedia(Client.Value.Guilds.GetLogoUrl(guild.Id).ToString())))
                .WithSeparator()
                .WithSection(content => content.WithTextDisplay(
                        queueCount == 0
                            ? $"**No maps** in the queue today! {EmojiSettings.Value.Congrats}"
                            : $"There are **{queueCount} maps** in the queue today {EmojiSettings.Value.NeedConfirmation}")
                    .WithAccessory(new ButtonBuilder()
                        .WithLabel("Check the queue")
                        .WithCustomId($"ranking_menu_queue:{contextId},1")
                        .WithStyle(ButtonStyle.Success)
                        .WithDisabled(true)))) // Queue navigation isn't implemented yet.
            .WithActionRow(actionRow => actionRow
                .WithButton(new ButtonBuilder()
                    .WithLabel("See previous rank/edits")
                    .WithCustomId($"ranking_menu_maps:{contextId},1")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(true))) // History navigation isn't implemented yet.
            .Build();

        switch (Context.Interaction)
        {
            case SocketMessageComponent messageComponent:
                await messageComponent.UpdateAsync(x => x.Components = component);
                break;
            case SocketSlashCommand:
                await RespondAsync(ephemeral: true, components: component);
                break;
        }
    }
}