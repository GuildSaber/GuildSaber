using System.Diagnostics;
using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Levels.Playlists.Http;
using GuildSaber.Common.Helpers;
using GuildSaber.CSharpClient.Routes.Guilds.Levels.Playlists;
using GuildSaber.DiscordBot.AutocompleteHandlers;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [SlashCommand("playlist", "Get a playlist of maps for a level or all levels")]
    public async Task Playlist(
        [Summary("Context")] [Autocomplete(typeof(ContextAutocompleteHandler))] ContextId contextId,
        [Summary("Type")] PlaylistRequests.PlaylistFilter filter = PlaylistRequests.PlaylistFilter.None,
        [Summary("Category")] [Autocomplete(typeof(CategoryAutocompleteHandler))] int? categoryId = null,
        [Summary("Level", "The level number (order) to get the playlist for")] uint? levelOrder = null,
        [Summary("User", "The user to get the playlists for (you if empty)")] IUser? user = null,
        [Summary("Visibility")] EDisplayChoice displayChoice = EDisplayChoice.Visible)
    {
        await DeferAsync(ephemeral: displayChoice.ToEphemeral());

        var (player, guildExtended, levelsResult) = await (
            GetPlayerAsync(user).AsTask(),
            GetGuildExtendedAsync().AsTask(),
            Client.Value.Levels.GetByContextIdAsync(contextId, categoryId, hasCategory: categoryId is not null)
        ).WhenAll();

        if (!levelsResult.TryGetValue(out var levels, out var levelError))
        {
            await FollowupAsync(embed: GetPlaylistCommand.BuildErrorEmbed(levelError));
            return;
        }

        if (levels.Length == 0)
        {
            await FollowupAsync(
                embed: GetPlaylistCommand.BuildErrorEmbed("No levels found for the given context/category."));
            return;
        }

        await using var memoryStream = new MemoryStream();

        // Single level requested by order
        if (levelOrder is not null)
        {
            var level = levels.FirstOrDefault(l => l.Order == levelOrder.Value);
            if (level is null)
            {
                await FollowupAsync(embed: GetPlaylistCommand.BuildErrorEmbed(
                    $"Level with order {levelOrder} not found in this context/category."));
                return;
            }

            var playlistResult = await Client.Value.Playlists.GetByLevelIdAsync(level.Id, filter, player.Id);
            if (!playlistResult.TryGetValue(out var playlist, out var playlistError) && playlist is null)
            {
                await FollowupAsync(embed: GetPlaylistCommand.BuildErrorEmbed(playlistError));
                return;
            }

            await Client.Value.Playlists.WriteToStream(memoryStream, playlist.Value);

            var fileName = PlaylistUtilities.GetPlaylistFileName(level, guildExtended);
            await FollowupWithFileAsync(memoryStream, fileName, filter switch
            {
                PlaylistRequests.PlaylistFilter.None => "Playlist generated.",
                PlaylistRequests.PlaylistFilter.NoneWithPassedScores
                    => $"Playlist containing only unpassed songs of {user?.Username ?? Context.User.Username} generated.",
                PlaylistRequests.PlaylistFilter.NoneWithPassedNorPendingScores
                    => $"Playlist containing only unpassed and non-pending songs of {user?.Username ?? Context.User.Username} generated.",
                _ => throw new UnreachableException()
            });
            return;
        }

        // All levels requested
        if (!(await Client.Value.Playlists.GetAsync(levels, filter, player.Id))
            .TryGetValue(out var levelsWithPlaylists, out var playlistsError))
        {
            await FollowupAsync(embed: GetPlaylistCommand.BuildErrorEmbed(playlistsError));
            return;
        }

        await Client.Value.Playlists.WritePlaylistArchiveToStreamAsync(memoryStream, levelsWithPlaylists,
            guildExtended);

        var archiveName = PlaylistUtilities.GetPlaylistArchiveName(guildExtended, contextId, categoryId);
        await FollowupWithFileAsync(memoryStream, archiveName, filter switch
        {
            PlaylistRequests.PlaylistFilter.None => "Playlists generated.",
            PlaylistRequests.PlaylistFilter.NoneWithPassedScores
                => $"Playlists containing only unpassed songs of {user?.Username ?? Context.User.Username} generated.",
            PlaylistRequests.PlaylistFilter.NoneWithPassedNorPendingScores
                => $"Playlists containing only unpassed and non-pending songs of {user?.Username ?? Context.User.Username} generated.",
            _ => throw new UnreachableException()
        });
    }
}

file static class GetPlaylistCommand
{
    public static Embed BuildErrorEmbed(string message)
        => new EmbedBuilder
        {
            Title = "Playlist Generation Failed",
            Color = Color.DarkOrange,
            Description = message
        }.Build();
}