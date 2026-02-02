using System.Linq.Expressions;
using GuildSaber.Database.Models.Server.Guilds.Levels;
using GuildSaber.Database.Models.Server.RankedScores;

namespace GuildSaber.Api.Features.Guilds.Levels.Playlists;

public static class PlaylistMappers
{
    public static Expression<Func<RankedMapListLevel, PlaylistResponses.Playlist>> MapPlaylistExpression(
        string? syncURL, string? image) => self => new PlaylistResponses.Playlist
    {
        PlaylistTitle = self.Category == null
            ? $"{self.Guild.Info.SmallName} ({self.Context.Info.Name}) - {self.Info.Name}"
            : $"{self.Guild.Info.SmallName} ({self.Context.Info.Name}) - {self.Category.Info.Name} - {self.Info.Name}",
        PlaylistAuthor = $"{self.Guild.Info.Name} (GuildSaber)",
        PlaylistDescription = $"A playlist for the ranked map list level \"{self.Info.Name}\" in the guild" +
                              $" \"{self.Guild.Info.Name}\" on the context \"{self.Context.Info.Name}\".",
        CustomData = new PlaylistResponses.PlaylistCustomData(SyncURL: syncURL),
        Songs = self.RankedMaps
            .SelectMany(map => map.MapVersions)
            .Select(version => new PlaylistResponses.PlaylistSong(
                Hash: version.Song.Hash,
                Difficulties: new[]
                {
                    new PlaylistResponses.PlaylistDifficultyData(
                        Characteristic: version.SongDifficulty.GameMode.Name,
                        Name: version.SongDifficulty.Difficulty.ToString())
                }))
            .ToArray(),
        Image = image
    };

    public static Expression<Func<RankedMapListLevel, PlaylistResponses.Playlist>> MapPlaylistPassedScoreExpression(
        PlayerId playerId, string? syncURL, string? image) => self => new PlaylistResponses.Playlist
    {
        PlaylistTitle = self.Category == null
            ? $"{self.Guild.Info.SmallName} ({self.Context.Info.Name}) - {self.Info.Name} (Passed removed)"
            : $"{self.Guild.Info.SmallName} ({self.Context.Info.Name}) - {self.Category.Info.Name} - {self.Info.Name} (Passed removed)",
        PlaylistAuthor = $"{self.Guild.Info.Name} (GuildSaber)",
        PlaylistDescription = $"A playlist containing only the ranked maps for the level \"{self.Info.Name}\" in the" +
                              $" guild \"{self.Guild.Info.Name}\" on the context \"{self.Context.Info.Name}\" that you" +
                              $" (playerId {playerId}) do not have a score on.",
        CustomData = new PlaylistResponses.PlaylistCustomData(SyncURL: syncURL),
        Songs = self.RankedMaps
            .Where(map => !map.RankedScores.Any(score =>
                score.PlayerId == playerId
                && score.State.HasFlag(RankedScore.EState.Selected)
                && ((int)score.State & (int)RankedScore.EState.NonPointGivingNoPending) == 0))
            .SelectMany(map => map.MapVersions)
            .Select(version => new PlaylistResponses.PlaylistSong(
                Hash: version.Song.Hash,
                Difficulties: new[]
                {
                    new PlaylistResponses.PlaylistDifficultyData(
                        Characteristic: version.SongDifficulty.GameMode.Name,
                        Name: version.SongDifficulty.Difficulty.ToString())
                }))
            .ToArray(),
        Image = image
    };

    public static Expression<Func<RankedMapListLevel, PlaylistResponses.Playlist>>
        MapPlaylistPassedOrPendingScoreExpression(
            PlayerId playerId, string? syncURL, string? image) => self => new PlaylistResponses.Playlist
    {
        PlaylistTitle = self.Category == null
            ? $"{self.Guild.Info.SmallName} ({self.Context.Info.Name}) - {self.Info.Name} (Passed or Pending removed)"
            : $"{self.Guild.Info.SmallName} ({self.Context.Info.Name}) - {self.Category.Info.Name} - {self.Info.Name} (Passed or Pending removed)",
        PlaylistAuthor = $"{self.Guild.Info.Name} (GuildSaber)",
        PlaylistDescription = $"A playlist containing only the ranked maps for the level \"{self.Info.Name}\" in the" +
                              $" guild \"{self.Guild.Info.Name}\" on the context \"{self.Context.Info.Name}\" that you" +
                              $" (playerId {playerId}) do not have a score on.",
        CustomData = new PlaylistResponses.PlaylistCustomData(SyncURL: syncURL),
        Songs = self.RankedMaps
            .Where(map => !map.RankedScores.Any(score =>
                score.PlayerId == playerId
                && score.State.HasFlag(RankedScore.EState.Selected)
                && ((int)score.State & (int)RankedScore.EState.NonPointGiving) == 0))
            .SelectMany(map => map.MapVersions)
            .Select(version => new PlaylistResponses.PlaylistSong(
                Hash: version.Song.Hash,
                Difficulties: new[]
                {
                    new PlaylistResponses.PlaylistDifficultyData(
                        Characteristic: version.SongDifficulty.GameMode.Name,
                        Name: version.SongDifficulty.Difficulty.ToString())
                }))
            .ToArray(),
        Image = image
    };
}