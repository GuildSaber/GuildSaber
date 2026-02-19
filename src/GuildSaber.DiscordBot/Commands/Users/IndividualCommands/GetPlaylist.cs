using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Levels.Playlists;
using GuildSaber.CSharpClient;
using GuildSaber.DiscordBot.AutocompleteHandlers;
using GuildSaber.DiscordBot.Core.Extensions;
using static GuildSaber.Api.Features.Guilds.Levels.LevelResponses;
using static GuildSaber.Api.Features.Guilds.Levels.Playlists.PlaylistResponses;
using static GuildSaber.Api.Features.Guilds.Categories.CategoryResponses;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [SlashCommand("playlist", "Get a playlist of maps for a level or all levels")]
    public async Task Playlist(
        [Summary("Context")] [Autocomplete(typeof(ContextAutocompleteHandler))] int contextId,
        [Summary("Type")] PlaylistRequests.PlaylistFilter filter = PlaylistRequests.PlaylistFilter.None,
        [Summary("Category")] [Autocomplete(typeof(CategoryAutocompleteHandler))] int? categoryId = null,
        [Summary("Level", "The level number (order) to get the playlist for")] uint? levelOrder = null,
        [Summary("User", "The user to get the playlists for (you if empty)")] IUser? user = null,
        [Summary("Visibility")] EDisplayChoice displayChoice = EDisplayChoice.Visible)
    {
        await DeferAsync(ephemeral: displayChoice.ToEphemeral());

        var playerTask = user is null
            ? GetPlayerAtMeAsync().AsTask()
            : GetPlayerAsync(user.DiscordId).AsTask();
        var guildExtendedTask = GetGuildExtendedAsync().AsTask();
        var levelsTask = Client.Value.Levels.GetByContextIdAsync(contextId, categoryId,
            hasCategory: categoryId is not null);
        var categoriesTask = Cache.GetGuildCategoriesAsync(await GetGuildIdAsync(), Client.Value).AsTask();

        await Task.WhenAll(playerTask, guildExtendedTask, levelsTask, categoriesTask);

        var (playerId, guildExtended, categories) = (
            playerTask.Result.Id,
            guildExtendedTask.Result,
            categoriesTask.Result
        );

        if (!levelsTask.Result.TryGetValue(out var levels, out var levelError))
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

            var playlistResult = await Client.Value.Playlists.GetByLevelIdAsync(level.Id, filter, playerId);
            if (!playlistResult.TryGetValue(out var playlist, out var playlistError))
            {
                await FollowupAsync(embed: GetPlaylistCommand.BuildErrorEmbed(playlistError));
                return;
            }

            await using var stream = GetPlaylistCommand.SerializePlaylist(playlist.Value);
            var fileName = GetPlaylistCommand.BuildFileName(level,
                guildSmallName: guildExtended.Guild.Info.SmallName,
                contextName: guildExtended.Contexts.First(x => x.Id == contextId).Info.Name,
                categoryName: categories.FirstOrDefault(c => c.Id == categoryId).Info.Name
            );
            await FollowupWithFileAsync(stream, fileName, filter switch
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
        await using var zipStream = await GetPlaylistCommand.BuildPlaylistZipAsync(Client.Value, levels,
            filter, playerId,
            guildSmallName: guildExtended.Guild.Info.SmallName,
            contextName: guildExtended.Contexts.First(x => x.Id == contextId).Info.Name,
            categories: categories
        );
        if (zipStream is null)
        {
            await FollowupAsync(
                embed: GetPlaylistCommand.BuildErrorEmbed("No playlists could be generated (all levels empty)."));
            return;
        }

        var archiveName = GetPlaylistCommand.BuildArchiveName(
            guildSmallName: guildExtended.Guild.Info.SmallName,
            contextName: guildExtended.Contexts.First(x => x.Id == contextId).Info.Name,
            categoryName: categories.FirstOrDefault(c => c.Id == categoryId).Info.Name
        );
        await FollowupWithFileAsync(zipStream, archiveName, filter switch
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
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static async Task<MemoryStream?> BuildPlaylistZipAsync(
        GuildSaberClient client, Level[] levels, PlaylistRequests.PlaylistFilter filter, PlayerId playerId,
        string guildSmallName, string contextName, Category[] categories)
    {
        var playlistTasks = levels
            .Select(async level =>
            {
                var result = await client.Playlists.GetByLevelIdAsync(level.Id, filter, playerId);
                return (Level: level, Playlist: result.IsSuccess ? result.Value : null);
            })
            .ToArray();

        await Task.WhenAll(playlistTasks);

        var playlistsWithLevels = playlistTasks
            .Select(t => t.Result)
            .Where(x => x.Playlist is { Songs.Length: > 0 })
            .ToArray();

        if (playlistsWithLevels.Length == 0)
            return null;

        var memoryStream = new MemoryStream();
        await using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (level, playlist) in playlistsWithLevels)
            {
                var categoryName = level.CategoryId is not null
                    ? categories.FirstOrDefault(c => c.Id == level.CategoryId).Info.Name
                    : null;
                var fileName = BuildFileName(level, guildSmallName, contextName, categoryName);
                var entry = zipArchive.CreateEntry(fileName, CompressionLevel.Optimal);
                await using var entryStream = await entry.OpenAsync();
                await using var playlistStream = SerializePlaylist(playlist!.Value);
                await playlistStream.CopyToAsync(entryStream);
            }
        }

        memoryStream.Position = 0;
        return memoryStream;
    }

    public static MemoryStream SerializePlaylist(in Playlist playlist)
    {
        var json = JsonSerializer.Serialize(playlist, _jsonOptions);
        return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }

    public static string BuildFileName(Level level, string guildSmallName, string contextName, string? categoryName)
        => !string.IsNullOrEmpty(categoryName)
            ? $"{level.Order:000}_{SanitizeFileName(guildSmallName)} ({SanitizeFileName(contextName)})_{SanitizeFileName(categoryName)}_{SanitizeFileName(level.Info.Name)}.bplist"
            : $"{level.Order:000}_{SanitizeFileName(guildSmallName)} ({SanitizeFileName(contextName)})_{SanitizeFileName(level.Info.Name)}.bplist";

    public static string BuildArchiveName(string guildSmallName, string contextName, string? categoryName)
        => !string.IsNullOrEmpty(categoryName)
            ? $"{SanitizeFileName(guildSmallName)} ({SanitizeFileName(contextName)})_{SanitizeFileName(categoryName)}.zip"
            : $"{SanitizeFileName(guildSmallName)} ({SanitizeFileName(contextName)}).zip";

    public static Embed BuildErrorEmbed(string message)
        => new EmbedBuilder
        {
            Title = "Playlist Generation Failed",
            Color = Color.DarkOrange,
            Description = message
        }.Build();

    private static string SanitizeFileName(string name)
        => string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
}