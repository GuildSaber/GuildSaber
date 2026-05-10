using System.IO.Compression;
using System.Text.Json;
using GuildSaber.Api.Features.Guilds;
using static GuildSaber.Api.Features.Guilds.Levels.Playlists.PlaylistResponses;
using static GuildSaber.Api.Features.Guilds.Levels.LevelResponses;

namespace GuildSaber.CSharpClient.Routes.Guilds.Levels.Playlists;

public static class PlaylistUtilities
{
    extension(PlaylistClient self)
    {
        public async Task WriteToFileAsync(string filePath, Playlist playlist)
        {
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await self.WriteToStream(fileStream, playlist);
        }

        /// <summary>
        /// Writes a playlist to a stream in JSON format using the client's JsonOptions.
        /// </summary>
        public async Task WriteToStream(Stream stream, Playlist playlist)
        {
            await JsonSerializer.SerializeAsync(stream, playlist, self.JsonOptions);
            if (stream.CanSeek)
                stream.Position = 0;
        }

        /// <summary>
        /// Writes multiple playlists to a zip archive stream, with each playlist as a separate entry named according to the level
        /// and guild information.
        /// </summary>
        public async Task WritePlaylistArchiveToStreamAsync(
            Stream stream, IEnumerable<PlaylistClient.LevelWithPlaylist> levelsWithPlaylists,
            GuildResponses.GuildExtended guildExtended)
        {
#if NET6_0_OR_GREATER
            await using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);
#else
            using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);
#endif
            foreach (var (level, playlist) in levelsWithPlaylists)
            {
                if (playlist is null)
                    continue;

                var fileName = GetPlaylistFileName(level, guildExtended);
                var entry = zipArchive.CreateEntry(fileName, CompressionLevel.Optimal);

#if NET6_0_OR_GREATER
                await using var entryStream = await entry.OpenAsync();
#else
                await using var entryStream = entry.Open();
#endif
                await self.WriteToStream(entryStream, playlist.Value);
            }

            if (stream.CanSeek)
                stream.Position = 0;
        }
    }

    public static string GetPlaylistFileName(Level level, GuildResponses.GuildExtended guildExtended)
    {
        var categoryName = guildExtended.Categories.FirstOrDefault(c => c.Id == level.CategoryId).Info.Name;
        var categoryPart = string.IsNullOrEmpty(categoryName) ? "" : $"{SanitizeFileName(categoryName)}_";

        var levelName = SanitizeFileName(level.Info.Name);
        var guildName = SanitizeFileName(guildExtended.Guild.Info.Name);
        var contextName = SanitizeFileName(guildExtended.Contexts.First(c => c.Id == level.ContextId).Info.Name);

        return $"{level.Order:000}_{guildName}_{contextName}_{categoryPart}{levelName}.bplist";
    }

    public static string GetPlaylistArchiveName(
        GuildResponses.GuildExtended guildExtended, ContextId contextId, int? categoryId)
    {
        var guildSmallName = SanitizeFileName(guildExtended.Guild.Info.Name);
        var contextName = SanitizeFileName(guildExtended.Contexts.First(c => c.Id == contextId).Info.Name);
        var categoryPart = guildExtended.Categories.Where(x => x.Id == categoryId)
            .Select(x => $"_{SanitizeFileName(x.Info.Name)}")
            .FirstOrDefault();

        return $"{guildSmallName} ({contextName}){categoryPart}.zip";
    }

    private static string SanitizeFileName(string name)
        => string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
}