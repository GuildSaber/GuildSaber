using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Guilds.Achievements.Http;
using GuildSaber.Api.Features.RankedScores.Http;
using GuildSaber.Database.Models.Server.Guilds.Achievements.Types;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.Server.Songs;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties.GameModes;
using GuildSaber.Database.Models.StrongTypes;

namespace GuildSaber.Api.Features.RankedMaps.Http;

public static class RankedMapMappers
{
    private static Func<RankedMap, RankedMapResponses.RankedMapSimple>? _mapRankedMapSimpleImpl;
    private static Func<RankedMap, RankedMapResponses.RankedMap>? _mapRankedMapImpl;
    private static Func<Song, RankedMapResponses.Song>? _mapSongImpl;
    private static Func<SongDifficulty, RankedMapResponses.SongDifficulty>? _mapSongDifficultyImpl;
    private static Func<MapVersion, RankedMapResponses.MapVersion>? _mapVersionImpl;

    [Expandable(nameof(MapRankedMapSimpleExpression))]
    public static RankedMapResponses.RankedMapSimple MapSimple(this RankedMap self)
        => (_mapRankedMapSimpleImpl ??= MapRankedMapSimpleExpression().Compile())(self);

    [Expandable(nameof(MapRankedMapExpression))]
    public static RankedMapResponses.RankedMap Map(this RankedMap self)
        => (_mapRankedMapImpl ??= MapRankedMapExpression().Compile())(self);

    public static Expression<Func<RankedMap, RankedMapResponses.RankedMapSimple>> MapRankedMapSimpleExpression()
        => self => new RankedMapResponses.RankedMapSimple(
            self.Id,
            self.ThreadId,
            self.GuildId,
            self.ContextId,
            self.Info.Map(),
            self.Requirements.Map(),
            self.Rating.Map(),
            self.MapVersions.Select(x => x.Map()).ToArray(),
            self.Categories.Select(x => (int)x.Id).ToArray());

    /// <warning>.AsExpandable() must be called with this expression</warning>
    public static Expression<Func<RankedMap, RankedMapResponses.RankedMap>> MapRankedMapExpression()
        => self => new RankedMapResponses.RankedMap(
            self.Id,
            self.ThreadId,
            self.GuildId,
            self.ContextId,
            self.Info.Map(),
            self.Requirements.Map(),
            self.Rating.Map(),
            self.MapVersions.Select(x => x.Map()).ToArray(),
            self.Categories.Select(x => (int)x.Id).ToArray(),
            self.Context.Achievements
                .Where(achievement =>
                    achievement is RankedMapListAchievement &&
                    self.Achievements.Any(candidate => candidate.Id == achievement.Id) ||
                    (achievement.CategoryId == null ||
                     self.Categories.Any(category => category.Id == achievement.CategoryId.Value)) &&
                    (achievement is DiffStarAchievement &&
                     self.Rating.DiffStar >= ((DiffStarAchievement)achievement).MinStar &&
                     (((DiffStarAchievement)achievement).MaxStar == null ||
                      self.Rating.DiffStar < ((DiffStarAchievement)achievement).MaxStar!.Value) ||
                     achievement is AccStarAchievement &&
                     self.Rating.AccStar >= ((AccStarAchievement)achievement).MinStar &&
                     (((AccStarAchievement)achievement).MaxStar == null ||
                      self.Rating.AccStar < ((AccStarAchievement)achievement).MaxStar!.Value)))
                .OrderBy(achievement => achievement.ProgressionOrder == null)
                .ThenBy(achievement => achievement.ProgressionOrder)
                .ThenBy(achievement => achievement.Id)
                .Select(achievement => achievement.Map())
                .ToArray());

    /// <warning>.AsExpandable() must be called with this expression</warning>
    public static Expression<Func<RankedMap, RankedMapResponses.RankedMapWithScores>> MapRankedMapWithScoresExpression(
        PlayerId playerId) => self => new RankedMapResponses.RankedMapWithScores(
        self.Map(),
        self.RankedScores.AsQueryable()
            .Where(x => x.PlayerId == playerId && x.IsSelected)
            .Select(x => x.Map())
            .ToArray()
    );

    public static RankedMapResponses.RankedMapInfo Map(this RankedMapInfo self) => new(
        CreatedAt: self.CreatedAt,
        EditedAt: self.EditedAt
    );

    public static RankedMapResponses.RankedMapRating Map(this RankedMapRating self) => new(
        AccStar: self.AccStar,
        DiffStar: self.DiffStar
    );

    public static RankedMapRequests.RankedMapRequirements Map(this RankedMapRequirements self) => new(
        NeedConfirmation: self.NeedConfirmation,
        NeedFullCombo: self.NeedFullCombo,
        MaxPauseDurationSec: self.MaxPauseDurationSec,
        ProhibitedModifiers: self.ProhibitedModifiers.Map(),
        MandatoryModifiers: self.MandatoryModifiers.Map(),
        MinAccuracy: self.MinAccuracy
    );

    [Expandable(nameof(MapSongExpression))]
    public static RankedMapResponses.Song Map(this Song self)
        => (_mapSongImpl ??= MapSongExpression().Compile())(self);

    public static Expression<Func<Song, RankedMapResponses.Song>> MapSongExpression() => self
        => new RankedMapResponses.Song(
            Id: self.Id,
            Hash: self.Hash,
            Key: self.BeatSaverKey,
            UploadedAt: self.UploadedAt,
            Info: new RankedMapResponses.SongInfo(
                BeatSaverName: self.Info.BeatSaverName,
                Name: self.Info.SongName,
                SubName: self.Info.SongSubName,
                AuthorName: self.Info.SongAuthorName,
                MapperName: self.Info.MapperName
            ),
            Stats: new RankedMapResponses.SongStats(
                BPM: self.Stats.BPM,
                DurationSec: self.Stats.DurationSec,
                IsAutoMapped: self.Stats.IsAutoMapped
            )
        );

    [Expandable(nameof(MapSongDifficultyExpression))]
    public static RankedMapResponses.SongDifficulty Map(this SongDifficulty self)
        => (_mapSongDifficultyImpl ??= MapSongDifficultyExpression().Compile())(self);

    public static Expression<Func<SongDifficulty, RankedMapResponses.SongDifficulty>> MapSongDifficultyExpression()
        => self => new RankedMapResponses.SongDifficulty(
            self.Id,
            self.BLLeaderboardId,
            self.SSLeaderboardId,
            self.Difficulty,
            self.GameMode.Name,
            new RankedMapResponses.SongDifficultyStats(
                self.Stats.MaxScore,
                self.Stats.NoteJumpSpeed,
                self.Stats.NoteCount,
                self.Stats.BombCount,
                self.Stats.ObstacleCount,
                self.Stats.NotesPerSecond,
                self.Stats.Duration));

    public static RankedMapResponses.SongDifficulty Map(this SongDifficulty self, GameMode gameMode) => new(
        Id: self.Id,
        BLLeaderboardId: self.BLLeaderboardId,
        SSLeaderboardId: self.SSLeaderboardId,
        Difficulty: self.Difficulty,
        GameMode: gameMode.Name,
        Stats: new RankedMapResponses.SongDifficultyStats(
            MaxScore: self.Stats.MaxScore,
            NJS: self.Stats.NoteJumpSpeed,
            NoteCount: self.Stats.NoteCount,
            BombCount: self.Stats.BombCount,
            ObstacleCount: self.Stats.ObstacleCount,
            NotesPerSecond: self.Stats.NotesPerSecond,
            Duration: self.Stats.Duration
        ));

    public static RankedMapResponses.MapVersion Map(
        this MapVersion self, Song song, SongDifficulty songDifficulty, GameMode gameMode) => new(
        AddedAt: self.AddedAt,
        Order: self.Order,
        Song: song.Map(),
        Difficulty: songDifficulty.Map(gameMode)
    );

    [Expandable(nameof(MapVersionExpression))]
    public static RankedMapResponses.MapVersion Map(this MapVersion self)
        => (_mapVersionImpl ??= MapVersionExpression().Compile())(self);

    public static Expression<Func<MapVersion, RankedMapResponses.MapVersion>> MapVersionExpression()
        => self => new RankedMapResponses.MapVersion(
            self.AddedAt,
            self.Order,
            self.Song.Map(),
            self.SongDifficulty.Map());

    public static Result<RankedMapRequirements, List<KeyValuePair<string, string[]>>> Map(
        this RankedMapRequests.RankedMapRequirements self)
    {
        List<KeyValuePair<string, string[]>> validationErrors = [];
        var accuracyResult = self.MinAccuracy.HasValue
            ? Accuracy.TryCreate(self.MinAccuracy.Value)
            : null as Result<Accuracy>?;

        var accuracy = default(Accuracy);
        if (accuracyResult is not null && !accuracyResult.Value.TryGetValue(out accuracy, out var accError))
            validationErrors.Add(new KeyValuePair<string, string[]>("MinAccuracy", [accError]));

        var prohibitedModifiersResult = self.ProhibitedModifiers.Map();
        if (!prohibitedModifiersResult.TryGetValue(out var prohibitedModifiers, out var probModError))
            validationErrors.Add(new KeyValuePair<string, string[]>("ProhibitedModifiers", [probModError]));

        var mandatoryModifiersResult = self.MandatoryModifiers.Map();
        if (!mandatoryModifiersResult.TryGetValue(out var mandatoryModifiers, out var mandModError))
            validationErrors.Add(new KeyValuePair<string, string[]>("MandatoryModifiers", [mandModError]));

        if (validationErrors.Count > 0)
            return Failure<RankedMapRequirements, List<KeyValuePair<string, string[]>>>(validationErrors);

        return new RankedMapRequirements(
            MinAccuracy: accuracyResult is null ? null : accuracy,
            ProhibitedModifiers: prohibitedModifiers,
            MandatoryModifiers: mandatoryModifiers,
            NeedConfirmation: self.NeedConfirmation,
            NeedFullCombo: self.NeedFullCombo,
            MaxPauseDurationSec: self.MaxPauseDurationSec
        );
    }

    public static RankedMapRequests.EModifiers Map(this AbstractScore.EModifiers self) =>
        Enum.GetValues<AbstractScore.EModifiers>()
            .Where(flag => flag != AbstractScore.EModifiers.None && self.HasFlag(flag))
            .Select(flag => flag switch
            {
                AbstractScore.EModifiers.NoObstacles => RankedMapRequests.EModifiers.NoObstacles,
                AbstractScore.EModifiers.NoBombs => RankedMapRequests.EModifiers.NoBombs,
                AbstractScore.EModifiers.NoFail => RankedMapRequests.EModifiers.NoFail,
                AbstractScore.EModifiers.SlowerSong => RankedMapRequests.EModifiers.SlowerSong,
                AbstractScore.EModifiers.BatteryEnergy => RankedMapRequests.EModifiers.BatteryEnergy,
                AbstractScore.EModifiers.InstaFail => RankedMapRequests.EModifiers.InstaFail,
                AbstractScore.EModifiers.SmallNotes => RankedMapRequests.EModifiers.SmallNotes,
                AbstractScore.EModifiers.ProMode => RankedMapRequests.EModifiers.ProMode,
                AbstractScore.EModifiers.FasterSong => RankedMapRequests.EModifiers.FasterSong,
                AbstractScore.EModifiers.StrictAngles => RankedMapRequests.EModifiers.StrictAngles,
                AbstractScore.EModifiers.DisappearingArrows => RankedMapRequests.EModifiers.DisappearingArrows,
                AbstractScore.EModifiers.GhostNotes => RankedMapRequests.EModifiers.GhostNotes,
                AbstractScore.EModifiers.NoArrows => RankedMapRequests.EModifiers.NoArrows,
                AbstractScore.EModifiers.SuperFastSong => RankedMapRequests.EModifiers.SuperFastSong,
                AbstractScore.EModifiers.OldDots => RankedMapRequests.EModifiers.OldDots,
                AbstractScore.EModifiers.OffPlatform => RankedMapRequests.EModifiers.OffPlatform,
                AbstractScore.EModifiers.ProhibitedDefaults => RankedMapRequests.EModifiers.ProhibitedDefaults,
                AbstractScore.EModifiers.None => RankedMapRequests.EModifiers.None,
                AbstractScore.EModifiers.Unk => RankedMapRequests.EModifiers.Unk,
                _ => throw new ArgumentOutOfRangeException(nameof(flag))
            })
            .Aggregate(RankedMapRequests.EModifiers.None, (acc, mapped) => acc | mapped);

    public static Result<AbstractScore.EModifiers> Map(this RankedMapRequests.EModifiers self) =>
        Enum.GetValues<RankedMapRequests.EModifiers>()
            .Where(flag => flag != RankedMapRequests.EModifiers.None && self.HasFlag(flag))
            .Select(flag => flag switch
            {
                RankedMapRequests.EModifiers.NoObstacles => AbstractScore.EModifiers.NoObstacles,
                RankedMapRequests.EModifiers.NoBombs => AbstractScore.EModifiers.NoBombs,
                RankedMapRequests.EModifiers.NoFail => AbstractScore.EModifiers.NoFail,
                RankedMapRequests.EModifiers.SlowerSong => AbstractScore.EModifiers.SlowerSong,
                RankedMapRequests.EModifiers.BatteryEnergy => AbstractScore.EModifiers.BatteryEnergy,
                RankedMapRequests.EModifiers.InstaFail => AbstractScore.EModifiers.InstaFail,
                RankedMapRequests.EModifiers.SmallNotes => AbstractScore.EModifiers.SmallNotes,
                RankedMapRequests.EModifiers.ProMode => AbstractScore.EModifiers.ProMode,
                RankedMapRequests.EModifiers.FasterSong => AbstractScore.EModifiers.FasterSong,
                RankedMapRequests.EModifiers.StrictAngles => AbstractScore.EModifiers.StrictAngles,
                RankedMapRequests.EModifiers.DisappearingArrows => AbstractScore.EModifiers.DisappearingArrows,
                RankedMapRequests.EModifiers.GhostNotes => AbstractScore.EModifiers.GhostNotes,
                RankedMapRequests.EModifiers.NoArrows => AbstractScore.EModifiers.NoArrows,
                RankedMapRequests.EModifiers.SuperFastSong => AbstractScore.EModifiers.SuperFastSong,
                RankedMapRequests.EModifiers.OldDots => AbstractScore.EModifiers.OldDots,
                RankedMapRequests.EModifiers.OffPlatform => AbstractScore.EModifiers.OffPlatform,
                RankedMapRequests.EModifiers.ProhibitedDefaults => AbstractScore.EModifiers.ProhibitedDefaults,
                RankedMapRequests.EModifiers.None => AbstractScore.EModifiers.None,
                RankedMapRequests.EModifiers.Unk => AbstractScore.EModifiers.Unk,
                _ => throw new ArgumentOutOfRangeException(nameof(flag))
            })
            .Aggregate(AbstractScore.EModifiers.None, (acc, mapped) => acc | mapped);
}