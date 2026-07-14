using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.Api.Features.RankedScores.Http;
using GuildSaber.Api.Shared;
using GuildSaber.Common.Result;
using GuildSaber.DiscordBot.Core.AutocompleteHandlers;

namespace GuildSaber.DiscordBot.Commands.ScoringTeam;

public partial class ScoringTeamModuleSlash
{
    [ComponentInteraction("scoring:*")]
    [SlashCommand("scoring", "Open the scoring team menu")]
    public async Task ScoringTeamMenu([Autocomplete<ContextAutocompleteHandler>] ContextId contextId)
    {
        var guild = await GetGuildAsync();
        var pendingCount = (await Client.Value.RankedScores.GetAsync(contextId,
                new RankedScoreRequests.Filters(RankedScoreRequests.ERankedScoreType.Pending),
                new PaginatedRequestOptions<RankedScoreRequests.ERankedScoreSorter>(Page: 1, PageSize: 1)))
            .Unwrap()
            .TotalCount;

        var component = new ComponentBuilderV2()
            .WithContainer(container => container
                .WithAccentColor(Color.Gold)
                .WithSection(header => header
                    .WithTextDisplay("### Welcome to the Scoring Team Menu!")
                    .WithAccessory(new ThumbnailBuilder()
                        .WithMedia(Client.Value.Guilds.GetLogoUrl(guild.Id).ToString())))
                .WithSeparator()
                .WithSection(content => content.WithTextDisplay(
                        pendingCount == 0
                            ? $"**No scores** to review today! {EmojiSettings.Value.Congrats}"
                            : $"There are **{pendingCount} scores** to review today {EmojiSettings.Value.NeedConfirmation}")
                    .WithAccessory(new ButtonBuilder()
                        .WithLabel("Review scores")
                        .WithCustomId($"scoring_admin_conf:{contextId},1")
                        .WithStyle(ButtonStyle.Success)
                        .WithDisabled(pendingCount == 0))))
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