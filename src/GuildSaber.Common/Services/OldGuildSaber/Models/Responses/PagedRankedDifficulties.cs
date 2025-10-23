using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;

namespace GuildSaber.Common.Services.OldGuildSaber.Models.Responses;

public class PagedRankedDifficulties
{
    public required RankedMapData[] RankedMaps { get; init; }
    public required Metadata Metadata { get; init; }

    public struct RankedMapData
    {
        public required int MapId { get; init; }
        public required string? MapName { get; init; }
        public required string? MapSubName { get; init; }
        public required string? MapAuthorName { get; init; }
        public required string? Mapper { get; init; }
        public required BeatSaverKey? BeatSaverId { get; init; }
        public required SongHash BeatSaverHash { get; init; }
        public required string CoverURL { get; init; }
        public required bool IsAutoMapped { get; init; }
        public required float BPM { get; init; }
        public required int Duration { get; init; }
        public required string? UnixUploadedTime { get; init; }

        public required List<RankedDifficultyData> Difficulties { get; init; }
    }

    public struct RankedDifficultyData
    {
        public required int GameModeValue { get; init; }
        public required string? GameModeName { get; init; }
        public required EDifficulty BeatSaverDifficultyValue { get; init; }
        public required string BeatSaverDifficultyName { get; init; }
        public required int DifficultyId { get; init; }
        public required int LevelId { get; init; }
        public required int? GuildCategoryId { get; init; }
        public required int MaxScore { get; init; }
        public required float NoteJumpSpeed { get; init; }
        public required int NoteCount { get; init; }
        public required int BombCount { get; init; }
        public required int ObstacleCount { get; init; }
        public required float NotesPerSecond { get; init; }
        public required float Seconds { get; init; }
        public required int MinScoreRequirement { get; init; }
        public required string ProhibitedModifiers { get; init; }
        public required string MandatoryModifiers { get; init; }
        public required ERequirements Requirements { get; init; }
        public required float PassWeight { get; init; }
        public required float AccWeight { get; init; }
        public required float PureWeight { get; init; }
        public required string UnixRankedTime { get; init; }
        public required string UnixEditedTime { get; init; }
        public required bool HasBestReplay { get; init; }
        public required string? ReplayViewerURL { get; init; }
        public required int? BestReplayPlayerId { get; init; }
    }
}