using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Api.Features.Internal;
using GuildSaber.CSharpClient;
using GuildSaber.CSharpClient.Routes.Internal;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [SlashCommand("guilds", "Search for guilds by name")]
    public async Task Guilds(
        [Summary("search", "Search term to filter guilds by name")] string search,
        [Summary("page", "Page number for pagination")] int page = 1
    ) => await RespondAsync(embed: await GuildsCommand.GetGuilds(search, page, Client.Value));
}

/// <summary>
/// Static class used to hold functions for the current command.
/// </summary>
file static class GuildsCommand
{
    public static async Task<Embed> GetGuilds(string search, int page, GuildSaberClient client)
    {
        var embedBuilder = new EmbedBuilder();

        var pageOption = new PaginatedRequestOptions<GuildRequests.EGuildSorter>
        {
            Page = page,
            PageSize = 5,
            MaxPage = 20,
            Order = EOrder.Desc,
            SortBy = GuildRequests.EGuildSorter.Name
        };
        var guilds = await client.Guilds.GetAsync(search, pageOption, CancellationToken.None);
        if (!guilds.TryGetValue(out var pagedGuilds, out var error))
        {
            embedBuilder.Description = $"Error fetching guilds: {error}";
            return embedBuilder.Build();
        }

        embedBuilder.WithColor(Color.Blue);
        if (pagedGuilds.Data.Length == 0)
        {
            embedBuilder.Description = $"No guilds found for the given search term: {search}.";
            return embedBuilder.Build();
        }

        embedBuilder.Title =
            $"Guilds (Page {pagedGuilds.Page} of {pagedGuilds.TotalPages}) with search term: '{search}'";
        foreach (var guild in pagedGuilds.Data) embedBuilder.AddField(guild.Info.Name, guild.Info.Description);

        return embedBuilder.Build();
    }
}