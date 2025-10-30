using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;

namespace GuildSaber.Api.Features.RankedMaps;

public static class RankedMapResponses
{
    public record RankedMapInfo(DateTimeOffset CreatedAt, DateTimeOffset EditedAt);

    public record RankedMapRequirements(
        bool NeedConfirmation,
        bool NeedFullCombo,
        float? MaxPauseDurationSec,
        RankedMapRequest.EModifiers ProhibitedModifiers,
        RankedMapRequest.EModifiers MandatoryModifiers,
        float? MinAccuracy
    );

    public record RankedMapRating(float AccStar, float DiffStar);

    public record SongStats(
        float BPM,
        float DurationSec,
        bool IsAutoMapped
    );

    public record SongInfo(
        string BeatSaverName,
        string Name,
        string SubName,
        string AuthorName,
        string MapperName
    );

    public record Song(
        int Id,
        SongHash Hash,
        BeatSaverKey? Key,
        DateTimeOffset UploadedAt,
        SongInfo Info,
        SongStats Stats
    );

    public record SongDifficulty(
        BLLeaderboardId? BLLeaderboardId,
        SSLeaderboardId? SSLeaderboardId,
        EDifficulty Difficulty,
        string GameMode,
        SongDifficultyStats Stats
    );

    public record SongDifficultyStats(
        int MaxScore,
        float NJS,
        int NoteCount,
        int BombCount,
        int ObstacleCount,
        float NotesPerSecond,
        double Duration
    );

    public record MapVersion(
        DateTimeOffset AddedAt,
        byte Order,
        Song Song,
        SongDifficulty Difficulty
    );

    public record RankedMap(
        long Id,
        int GuildId,
        int ContextId,
        RankedMapInfo Info,
        RankedMapRequirements Requirements,
        RankedMapRating Rating,
        MapVersion[] Versions,
        int[] CategoryIds
    );
}