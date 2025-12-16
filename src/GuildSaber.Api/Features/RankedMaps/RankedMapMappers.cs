using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.Server.Songs;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties.GameModes;
using GuildSaber.Database.Models.StrongTypes;

namespace GuildSaber.Api.Features.RankedMaps;

public static class RankedMapMappers
{
    public static Expression<Func<RankedMap, RankedMapResponses.RankedMap>> MapRankedMapExpression
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

    public static RankedMapResponses.RankedMap Map(
        this RankedMap self, Song song, SongDifficulty songDifficulty, GameMode gameMode) => new(
        Id: self.Id,
        GuildId: self.GuildId,
        ContextId: self.ContextId,
        Info: self.Info.Map(),
        Requirements: self.Requirements.Map(),
        Rating: self.Rating.Map(),
        Versions: self.MapVersions.Select(v => v.Map(song, songDifficulty, gameMode)).ToArray(),
        CategoryIds: self.Categories.Select(x => (int)x.Id).ToArray(),
        LevelIds: self.Levels.Select(x => (int)x.Id).ToArray()
    );

    public static RankedMapResponses.RankedMapInfo Map(this RankedMapInfo self) => new(
        CreatedAt: self.CreatedAt,
        EditedAt: self.EditedAt
    );

    public static RankedMapResponses.RankedMapRating Map(this RankedMapRating self) => new(
        AccStar: self.AccStar,
        DiffStar: self.DiffStar
    );

    public static RankedMapResponses.RankedMapRequirements Map(this RankedMapRequirements self) => new(
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
        this RankedMapRequest.RankedMapRequirements self)
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

    public static RankedMapRequest.EModifiers Map(this AbstractScore.EModifiers self) =>
        Enum.GetValues<AbstractScore.EModifiers>()
            .Where(flag => flag != AbstractScore.EModifiers.None && self.HasFlag(flag))
            .Select(flag => flag switch
            {
                AbstractScore.EModifiers.NoObstacles => RankedMapRequest.EModifiers.NoObstacles,
                AbstractScore.EModifiers.NoBombs => RankedMapRequest.EModifiers.NoBombs,
                AbstractScore.EModifiers.NoFail => RankedMapRequest.EModifiers.NoFail,
                AbstractScore.EModifiers.SlowerSong => RankedMapRequest.EModifiers.SlowerSong,
                AbstractScore.EModifiers.BatteryEnergy => RankedMapRequest.EModifiers.BatteryEnergy,
                AbstractScore.EModifiers.InstaFail => RankedMapRequest.EModifiers.InstaFail,
                AbstractScore.EModifiers.SmallNotes => RankedMapRequest.EModifiers.SmallNotes,
                AbstractScore.EModifiers.ProMode => RankedMapRequest.EModifiers.ProMode,
                AbstractScore.EModifiers.FasterSong => RankedMapRequest.EModifiers.FasterSong,
                AbstractScore.EModifiers.StrictAngles => RankedMapRequest.EModifiers.StrictAngles,
                AbstractScore.EModifiers.DisappearingArrows => RankedMapRequest.EModifiers.DisappearingArrows,
                AbstractScore.EModifiers.GhostNotes => RankedMapRequest.EModifiers.GhostNotes,
                AbstractScore.EModifiers.NoArrows => RankedMapRequest.EModifiers.NoArrows,
                AbstractScore.EModifiers.SuperFastSong => RankedMapRequest.EModifiers.SuperFastSong,
                AbstractScore.EModifiers.OldDots => RankedMapRequest.EModifiers.OldDots,
                AbstractScore.EModifiers.OffPlatform => RankedMapRequest.EModifiers.OffPlatform,
                AbstractScore.EModifiers.ProhibitedDefaults => RankedMapRequest.EModifiers.ProhibitedDefaults,
                AbstractScore.EModifiers.None => RankedMapRequest.EModifiers.None,
                AbstractScore.EModifiers.Unk => RankedMapRequest.EModifiers.Unk,
                _ => throw new ArgumentOutOfRangeException(nameof(flag))
            })
            .Aggregate(RankedMapRequest.EModifiers.None, (acc, mapped) => acc | mapped);

    public static Result<AbstractScore.EModifiers> Map(this RankedMapRequest.EModifiers self) =>
        Enum.GetValues<RankedMapRequest.EModifiers>()
            .Where(flag => flag != RankedMapRequest.EModifiers.None && self.HasFlag(flag))
            .Select(flag => flag switch
            {
                RankedMapRequest.EModifiers.NoObstacles => AbstractScore.EModifiers.NoObstacles,
                RankedMapRequest.EModifiers.NoBombs => AbstractScore.EModifiers.NoBombs,
                RankedMapRequest.EModifiers.NoFail => AbstractScore.EModifiers.NoFail,
                RankedMapRequest.EModifiers.SlowerSong => AbstractScore.EModifiers.SlowerSong,
                RankedMapRequest.EModifiers.BatteryEnergy => AbstractScore.EModifiers.BatteryEnergy,
                RankedMapRequest.EModifiers.InstaFail => AbstractScore.EModifiers.InstaFail,
                RankedMapRequest.EModifiers.SmallNotes => AbstractScore.EModifiers.SmallNotes,
                RankedMapRequest.EModifiers.ProMode => AbstractScore.EModifiers.ProMode,
                RankedMapRequest.EModifiers.FasterSong => AbstractScore.EModifiers.FasterSong,
                RankedMapRequest.EModifiers.StrictAngles => AbstractScore.EModifiers.StrictAngles,
                RankedMapRequest.EModifiers.DisappearingArrows => AbstractScore.EModifiers.DisappearingArrows,
                RankedMapRequest.EModifiers.GhostNotes => AbstractScore.EModifiers.GhostNotes,
                RankedMapRequest.EModifiers.NoArrows => AbstractScore.EModifiers.NoArrows,
                RankedMapRequest.EModifiers.SuperFastSong => AbstractScore.EModifiers.SuperFastSong,
                RankedMapRequest.EModifiers.OldDots => AbstractScore.EModifiers.OldDots,
                RankedMapRequest.EModifiers.OffPlatform => AbstractScore.EModifiers.OffPlatform,
                RankedMapRequest.EModifiers.ProhibitedDefaults => AbstractScore.EModifiers.ProhibitedDefaults,
                RankedMapRequest.EModifiers.None => AbstractScore.EModifiers.None,
                RankedMapRequest.EModifiers.Unk => AbstractScore.EModifiers.Unk,
                _ => throw new ArgumentOutOfRangeException(nameof(flag))
            })
            .Aggregate(AbstractScore.EModifiers.None, (acc, mapped) => acc | mapped);
}