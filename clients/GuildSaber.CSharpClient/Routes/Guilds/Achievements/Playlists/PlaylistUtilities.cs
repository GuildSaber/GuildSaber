using System.IO.Compression;
using System.Text.Json;
using GuildSaber.Api.Features.Guilds.Http;
using static GuildSaber.Api.Features.Guilds.Achievements.Playlists.Http.PlaylistResponses;
using static GuildSaber.Api.Features.Guilds.Achievements.Http.AchievementResponses;

namespace GuildSaber.CSharpClient.Routes.Guilds.Achievements.Playlists;

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
        /// Writes multiple playlists to a zip archive stream, with each playlist as a separate entry named according to the
        /// achievement
        /// and guild information.
        /// </summary>
        public async Task WritePlaylistArchiveToStreamAsync(
            Stream stream, IEnumerable<PlaylistClient.AchievementWithPlaylist> achievementsWithPlaylists,
            GuildResponses.GuildExtended guildExtended)
        {
#if NET6_0_OR_GREATER
            await using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
#else
            using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
#endif
            {
                foreach (var (achievement, playlist) in achievementsWithPlaylists)
                {
                    if (playlist is null)
                        continue;

                    var fileName = GetPlaylistFileName(achievement, guildExtended);
                    var entry = zipArchive.CreateEntry(fileName, CompressionLevel.Optimal);

#if NET6_0_OR_GREATER
                    await using var entryStream = await entry.OpenAsync();
#else
                    await using var entryStream = entry.Open();
#endif
                    await self.WriteToStream(entryStream, playlist.Value);
                }
            }

            if (stream.CanSeek)
                stream.Position = 0;
        }
    }

    public static string GetPlaylistFileName(Achievement achievement, GuildResponses.GuildExtended guildExtended)
    {
        var categoryName = guildExtended.Categories.FirstOrDefault(c => c.Id == achievement.CategoryId).Info.Name;
        var categoryPart = string.IsNullOrEmpty(categoryName) ? "" : $"{SanitizeFileName(categoryName)}_";

        var achievementName = SanitizeFileName(achievement.Info.Name);
        var guildName = SanitizeFileName(guildExtended.Guild.Info.Name);
        var contextName = SanitizeFileName(guildExtended.Contexts.First(c => c.Id == achievement.ContextId).Info.Name);
        var progressionPart = achievement.Progression is AchievementProgression.Ordered progression
            ? $"{progression.Order:000}_"
            : string.Empty;

        return $"{progressionPart}{guildName}_{contextName}_{categoryPart}{achievementName}.bplist";
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

    public static string SanitizeFileName(string name)
        => string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
}