using System.Text;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Api.Features.Internal;
using GuildSaber.CSharpClient;
using GuildSaber.CSharpClient.Routes.Internal;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Settings;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [SlashCommand("search_guild", "Search for guilds by a query")]
    public async Task Guilds(
        [Summary("search", "The search term to find guilds")] string? search = null,
        [Summary("page", "Page number for pagination")] int page = 1,
        [Summary("Visibility")] EDisplayChoice displayChoice = EDisplayChoice.Secret
    ) => await RespondAsync(ephemeral: displayChoice.ToEphemeral(), components: (await SearchGuildsCommand
            .GetGuildsComponentAsync(search, page, Client.Value, LinkSettings.Value))
        .Build());

    [ComponentInteraction("guilds_prev_*_*")]
    public async Task GuildsPreviousPage(string search, int page)
    {
        var component = (await SearchGuildsCommand
                .GetGuildsComponentAsync(search, page, Client.Value, LinkSettings.Value))
            .Build();
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(msg => msg.Components = component);
    }

    [ComponentInteraction("guilds_next_*_*")]
    public async Task GuildsNextPage(string search, int page)
    {
        var component = (await SearchGuildsCommand
                .GetGuildsComponentAsync(search, page, Client.Value, LinkSettings.Value))
            .Build();
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(msg => msg.Components = component);
    }
}

/// <summary>
/// Static class used to hold functions for the current command.
/// </summary>
file static class SearchGuildsCommand
{
    public static async Task<ComponentBuilderV2> GetGuildsComponentAsync(
        string? search, int page, GuildSaberClient client, LinkSettings linkSettings)
    {
        var pageOption = new PaginatedRequestOptions<GuildRequests.EGuildSorter>
        {
            Page = page,
            PageSize = 5,
            MaxPage = 20,
            Order = EOrder.Desc,
            SortBy = string.IsNullOrWhiteSpace(search)
                ? GuildRequests.EGuildSorter.Popularity
                : GuildRequests.EGuildSorter.Name
        };

        var guilds = await client.Guilds.GetAsync(search, pageOption);

        return !guilds.TryGetValue(out var pagedGuilds, out var error)
            ? new ComponentBuilderV2().WithTextDisplay($"Error fetching guilds: {error}")
            : BuildGuildsComponent(pagedGuilds, search, page, linkSettings);
    }

    private static ComponentBuilderV2 BuildGuildsComponent(
        in PagedList<GuildResponses.Guild> pagedGuilds,
        string? search,
        int page,
        LinkSettings linkSettings)
    {
        var builder = new ComponentBuilderV2();

        if (pagedGuilds.Data.Length == 0)
            return builder.WithTextDisplay(pagedGuilds.Page != 1
                ? $"(Page {pagedGuilds.Page}), No guilds found for the search term: {search}.\n" +
                  "You might want to go back to page 1."
                : $"No guilds found for the search term: {search}.");

        foreach (var guild in pagedGuilds.Data)
            builder.WithContainer(BuildGuildDisplayContainer(guild, linkSettings));

        if (pagedGuilds.TotalCount == pagedGuilds.Data.Length)
            return builder.WithTextDisplay($"Found **{pagedGuilds.TotalCount}** guilds" +
                                           $"{(search is null ? "" : $" for the search term: '{search}'")}.");

        builder.WithTextDisplay($"(Page: **{pagedGuilds.Page}**/{pagedGuilds.TotalPages}) " +
                                $"Found **{pagedGuilds.TotalCount}** guilds" +
                                $"{(search is null ? "" : $" for the search term: '{search}'")}.");

        var searchParam = search ?? string.Empty;
        builder.WithActionRow(new ActionRowBuilder()
            .WithButton($"guilds_prev_{searchParam}_{page - 1}" switch
            {
                { Length: > 100 } => new ButtonBuilder()
                    .WithLabel("Previous Page (search term too long!)")
                    .WithStyle(ButtonStyle.Danger)
                    .WithDisabled(true),
                var id => new ButtonBuilder()
                    .WithLabel("Previous Page")
                    .WithStyle(ButtonStyle.Primary)
                    .WithCustomId(id)
                    .WithDisabled(pagedGuilds.Page <= 1)
            })
            .WithButton($"guilds_next_{searchParam}_{page + 1}" switch
            {
                { Length: > 100 } => new ButtonBuilder()
                    .WithLabel("Next Page (search term too long!)")
                    .WithStyle(ButtonStyle.Danger)
                    .WithDisabled(true),
                var id => new ButtonBuilder()
                    .WithLabel("Next Page")
                    .WithStyle(ButtonStyle.Primary)
                    .WithCustomId(id)
                    .WithDisabled(pagedGuilds.Page >= pagedGuilds.TotalPages)
            }));

        return builder;
    }

    private static ContainerBuilder BuildGuildDisplayContainer(GuildResponses.Guild guild, LinkSettings linkSettings)
    {
        var sb = new StringBuilder()
            .Append("## ").Append(guild.Info.Name)
            .Append(" **[").Append(guild.Info.SmallName).AppendLine("]**")
            .AppendLine()
            .AppendLine(guild.Info.Description)
            .AppendLine();

        sb.Append("Status: **").Append(guild.Status).AppendLine("**");

        if (guild.Requirements is { MinRank: not null } or { MaxRank: not null })
        {
            sb.Append("Rank: ");
            if (guild.Requirements.MinRank is { } minRank)
                sb.Append(minRank).Append(" - ");
            if (guild.Requirements.MaxRank is { } maxRank)
                sb.Append(maxRank);
            sb.AppendLine();
        }

        if (guild.Requirements is { MinPP: not null } or { MaxPP: not null })
        {
            sb.Append("PP: ");
            if (guild.Requirements.MinPP is { } minPP)
                sb.Append(minPP).Append(" - ");
            if (guild.Requirements.MaxPP is { } maxPP)
                sb.Append(maxPP);
            sb.AppendLine();
        }

        if (guild.DiscordInfo.InviteCode is { } inviteCode)
            sb.AppendLine()
                .Append("[Join Discord Server](https://discord.gg/")
                .Append(inviteCode).Append(')');

        var container = new ContainerBuilder()
            .WithAccentColor(Color.FromArgb(guild.Info.Color))
            .WithSection(sectionBuilder => sectionBuilder
                .WithAccessory(
                    new ThumbnailBuilder().WithMedia($"{linkSettings.CdnBaseUri}guilds/{guild.Id}/logo.jpg"))
                .WithTextDisplay(sb.ToString()));

        return container;
    }
}