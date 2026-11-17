using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Members.Http;
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
        var (guild, (queueResult, allowanceResult, boostsResult, viewerResult)) = await (
            Context.Interaction.HasResponded ? Task.CompletedTask : DeferAsync(ephemeral: true),
            GetGuildAsync().AsTask(),
            (
                Client.Value.MapRequests.GetQueueAsync(contextId, new MapProposalRequests.Filters(),
                    new PaginatedRequestOptions<MapProposalRequests.EMapRequestSorter>(Page: 1, PageSize: 1)),
                Client.Value.MapRequests.GetAllowanceAsync(contextId),
                Client.Value.MapRequests.GetBoostsAsync(),
                Client.Value.Players.GetExtendedAtMeAsync()
            ).WhenAll()
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
                    .WithTextDisplay("### Ranking menu")
                    .WithAccessory(new ThumbnailBuilder()
                        .WithMedia(Client.Value.Guilds.GetLogoUrl(guild.Id).ToString())))
                .WithSeparator()
                .WithActionRow(row => row
                    .WithButton($"Open queue ({queue.TotalCount})",
                        $"ranking_menu_queue:{contextId},1,{(int)MapProposalRequests.EMapRequestSorter.Priority},{(int)ERankingThreadOrigin.Queue}")
                    .WithButton("New request", $"ranking_new:{contextId}", ButtonStyle.Success,
                        disabled: allowance is null || allowance.Drafts >= allowance.MaxDrafts))
                .WithActionRow(row => row
                    .WithButton(allowance is null
                            ? "My drafts"
                            : $"My drafts ({allowance.Drafts}/{allowance.MaxDrafts})",
                        $"ranking_menu_drafts:{contextId},1", disabled: allowance is null)
                    .WithButton(boosts is null
                            ? "My boosted threads"
                            : $"My boosted threads ({boosts.Allocations.Count(x => x.ContextId == contextId)})",
                        $"ranking_menu_queue:{contextId},1,{(int)MapProposalRequests.EMapRequestSorter.Priority},{(int)ERankingThreadOrigin.Boosts}",
                        ButtonStyle.Secondary, disabled: boosts is null)
                    .WithButton("Refresh", $"ranking:{contextId}", ButtonStyle.Secondary))
                .WithTextDisplay("-# Find ranked maps with /search to open their threads."));

        if (allowance is null)
            component.WithTextDisplay("Draft limits unavailable. Link your Discord account on GuildSaber or refresh.");

        var viewer = viewerResult.GetValueOrDefault();
        var permissions = viewer?.Members.FirstOrDefault(x => x.GuildId == guild.Id)?.Permissions
                          ?? MemberResponses.EPermission.None;
        if (viewer?.Player.IsManager is true ||
            (permissions & (MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader)) != 0)
            component.WithContainer(container => container.WithAccentColor(Color.Red)
                .WithActionRow(row => row
                    .WithButton("Manage achievements", $"ranking_achievements:{contextId},-1,1,1",
                        ButtonStyle.Secondary)
                    .WithButton("Manage categories", $"ranking_categories:{contextId},1", ButtonStyle.Secondary)));

        await ModifyOriginalResponseAsync(x =>
        {
            x.Flags = MessageFlags.ComponentsV2;
            x.Components = component.Build();
            x.AllowedMentions = AllowedMentions.None;
        });
    }
}