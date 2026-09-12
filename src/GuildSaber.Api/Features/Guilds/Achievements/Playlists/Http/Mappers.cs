using System.Linq.Expressions;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Achievements;
using GuildSaber.Database.Models.Server.Guilds.Achievements.Types;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions;
using GuildSaber.Database.Models.Server.RankedScores;

namespace GuildSaber.Api.Features.Guilds.Achievements.Playlists.Http;

public static class PlaylistMappers
{
    private static Expression<Func<Achievement, IQueryable<MapVersion>>> MapVersionsExpression(
        ServerDbContext dbContext) => self => dbContext.MapVersions.Where(version =>
        dbContext.RankedMaps.Any(map =>
            map.Id == version.RankedMapId &&
            map.ContextId == self.ContextId &&
            (self is RankedMapListAchievement &&
             map.Achievements.Any(achievement => achievement.Id == self.Id) ||
             (self.CategoryId == null ||
              map.Categories.Any(category => category.Id == self.CategoryId.Value)) &&
             (self is DiffStarAchievement &&
              map.Rating.DiffStar >= ((DiffStarAchievement)self).MinStar &&
              (((DiffStarAchievement)self).MaxStar == null ||
               map.Rating.DiffStar < ((DiffStarAchievement)self).MaxStar!.Value) ||
              self is AccStarAchievement &&
              map.Rating.AccStar >= ((AccStarAchievement)self).MinStar &&
              (((AccStarAchievement)self).MaxStar == null ||
               map.Rating.AccStar < ((AccStarAchievement)self).MaxStar!.Value)))));

    public static Expression<Func<Achievement, PlaylistResponses.Playlist>> MapPlaylistExpression(
        ServerDbContext dbContext, string? syncURL, string? image) => self => new PlaylistResponses.Playlist
    {
        PlaylistTitle = self.Category == null
            ? $"{self.Guild.Info.SmallName} ({self.Context.Info.Name}) - {self.Info.Name}"
            : $"{self.Guild.Info.SmallName} ({self.Context.Info.Name}) - {self.Category.Info.Name} - {self.Info.Name}",
        PlaylistAuthor = $"{self.Guild.Info.Name} (GuildSaber)",
        PlaylistDescription = $"A playlist for the achievement \"{self.Info.Name}\" in the guild" +
                              $" \"{self.Guild.Info.Name}\" on the context \"{self.Context.Info.Name}\".",
        CustomData = new PlaylistResponses.PlaylistCustomData(SyncURL: syncURL),
        Songs = MapVersionsExpression(dbContext).Invoke(self)
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

    public static Expression<Func<Achievement, PlaylistResponses.Playlist>> MapPlaylistPassedScoreExpression(
        ServerDbContext dbContext, PlayerId playerId, string? syncURL, string? image) => self
        => new PlaylistResponses.Playlist
        {
            PlaylistTitle = self.Category == null
                ? $"{self.Guild.Info.SmallName} ({self.Context.Info.Name}) - {self.Info.Name} (Passed removed)"
                : $"{self.Guild.Info.SmallName} ({self.Context.Info.Name}) - {self.Category.Info.Name} - {self.Info.Name} (Passed removed)",
            PlaylistAuthor = $"{self.Guild.Info.Name} (GuildSaber)",
            PlaylistDescription =
                $"A playlist containing only the ranked maps for the achievement \"{self.Info.Name}\" in the" +
                $" guild \"{self.Guild.Info.Name}\" on the context \"{self.Context.Info.Name}\" that you" +
                $" (playerId {playerId}) do not have a score on.",
            CustomData = new PlaylistResponses.PlaylistCustomData(SyncURL: syncURL),
            Songs = MapVersionsExpression(dbContext).Invoke(self)
                .Where(version => !dbContext.RankedScores.Any(score =>
                    score.RankedMapId == version.RankedMapId
                    && score.PlayerId == playerId
                    && score.IsSelected
                    && score is PointGivingRankedScore))
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

    public static Expression<Func<Achievement, PlaylistResponses.Playlist>>
        MapPlaylistPassedOrPendingScoreExpression(
            ServerDbContext dbContext, PlayerId playerId, string? syncURL, string? image)
        => self => new PlaylistResponses.Playlist
        {
            PlaylistTitle = self.Category == null
                ? $"{self.Guild.Info.SmallName} ({self.Context.Info.Name}) - {self.Info.Name} (Passed or Pending removed)"
                : $"{self.Guild.Info.SmallName} ({self.Context.Info.Name}) - {self.Category.Info.Name} - {self.Info.Name} (Passed or Pending removed)",
            PlaylistAuthor = $"{self.Guild.Info.Name} (GuildSaber)",
            PlaylistDescription =
                $"A playlist containing only the ranked maps for the achievement \"{self.Info.Name}\" in the" +
                $" guild \"{self.Guild.Info.Name}\" on the context \"{self.Context.Info.Name}\" that you" +
                $" (playerId {playerId}) do not have a score on.",
            CustomData = new PlaylistResponses.PlaylistCustomData(SyncURL: syncURL),
            Songs = MapVersionsExpression(dbContext).Invoke(self)
                .Where(version => !dbContext.RankedScores.Any(score =>
                    score.RankedMapId == version.RankedMapId
                    && score.PlayerId == playerId
                    && score.IsSelected
                    && (score is PointGivingRankedScore || score is PendingRankedScore)))
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