using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.RankedScores.Http;
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
    private static Func<RankedMap, RankedMapResponses.RankedMap>? _mapRankedMapImpl;

    [Expandable(nameof(MapRankedMapExpression))]
    public static RankedMapResponses.RankedMap Map(this RankedMap self)
        => (_mapRankedMapImpl ??= MapRankedMapExpression().Compile())(self);

    public static Expression<Func<RankedMap, RankedMapResponses.RankedMap>> MapRankedMapExpression()
        => self => new RankedMapResponses.RankedMap(
            self.Id,
            self.GuildId,
            self.ContextId,
            self.Info.Map(),
            self.Requirements.Map(),
            self.Rating.Map(),
            self.MapVersions.Select(x => new RankedMapResponses.MapVersion(
                x.AddedAt,
                x.Order,
                new RankedMapResponses.Song(
                    x.Song.Id,
                    x.Song.Hash,
                    x.Song.BeatSaverKey,
                    x.Song.UploadedAt,
                    new RankedMapResponses.SongInfo(
                        x.Song.Info.BeatSaverName,
                        x.Song.Info.SongName,
                        x.Song.Info.SongSubName,
                        x.Song.Info.SongAuthorName,
                        x.Song.Info.MapperName
                    ),
                    new RankedMapResponses.SongStats(
                        x.Song.Stats.BPM,
                        x.Song.Stats.DurationSec,
                        x.Song.Stats.IsAutoMapped
                    )),
                new RankedMapResponses.SongDifficulty(
                    x.SongDifficultyId,
                    x.SongDifficulty.BLLeaderboardId,
                    x.SongDifficulty.SSLeaderboardId,
                    x.SongDifficulty.Difficulty,
                    x.SongDifficulty.GameMode.Name,
                    new RankedMapResponses.SongDifficultyStats(
                        x.SongDifficulty.Stats.MaxScore,
                        x.SongDifficulty.Stats.NoteJumpSpeed,
                        x.SongDifficulty.Stats.NoteCount,
                        x.SongDifficulty.Stats.BombCount,
                        x.SongDifficulty.Stats.ObstacleCount,
                        x.SongDifficulty.Stats.NotesPerSecond,
                        x.SongDifficulty.Stats.Duration
                    )))).ToArray(),
            self.Categories.Select(x => (int)x.Id).ToArray(),
            self.Levels.Select(x => (int)x.Id).ToArray());

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

    public static RankedMapResponses.Song Map(this Song self) => new(
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