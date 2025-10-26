using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;

namespace GuildSaber.Common.Services.ScoreSaber.Models.Responses;

public record PlayerScoreCollection
{
    public required Metadata Metadata { get; init; }
    public required PlayerScore[] PlayerScores { get; init; }
}

public record PlayerScore
{
    public required Score Score { get; init; }
    public required LeaderboardInfo Leaderboard { get; init; }
}

public class Score
{
    public required ScoreSaberScoreId Id { get; init; }
    public required LeaderboardPlayerInfo LeaderboardPlayerInfo { get; init; }
    public required int Rank { get; init; }
    public required int BaseScore { get; init; }
    public required int ModifiedScore { get; init; }
    public required float Pp { get; init; }
    public required float Weight { get; init; }
    public required string Modifiers { get; init; }
    public required float Multiplier { get; init; }
    public required int BadCuts { get; init; }
    public required int MissedNotes { get; init; }
    public required int MaxCombo { get; init; }
    public required bool FullCombo { get; init; }
    public required HMD Hmd { get; init; }
    public required bool HasReplay { get; init; }
    public required DateTimeOffset TimeSet { get; init; }
    public required string? DeviceHmd { get; init; }
    public required string? DeviceControllerLeft { get; init; }
    public required string? DeviceControllerRight { get; init; }
}

public record LeaderboardInfo
{
    public required SSLeaderboardId Id { get; init; }
    public required SongHash SongHash { get; init; }
    public required string SongName { get; init; }
    public required string SongSubName { get; init; }
    public required string SongAuthorName { get; init; }
    public required string LevelAuthorName { get; init; }
    public required DifficultyResponse Difficulty { get; init; }
    public required int MaxScore { get; init; }

    public required DateTimeOffset CreatedDate { get; init; }
    public required DateTimeOffset? RankedDate { get; init; }
    public required DateTimeOffset? QualifiedDate { get; init; }
    public required DateTimeOffset? LovedDate { get; init; }

    public required bool Ranked { get; init; }
    public required bool Qualified { get; init; }
    public required bool Loved { get; init; }

    public required float MaxPP { get; init; }
    public required float Stars { get; init; }
    public required bool PositiveModifiers { get; init; }
    public required int Plays { get; init; }
    public required int DailyPlays { get; init; }
    public required string CoverImage { get; init; }

    public required PlayerScore? PlayerScore { get; init; }
    public required DifficultyResponse[] Difficulties { get; init; }
}

public record DifficultyResponse
{
    public required int LeaderboardId { get; init; }
    public required EDifficulty Difficulty { get; init; }
    public required string GameMode { get; init; }
    public required string DifficultyRaw { get; init; }
}

public class LeaderboardPlayerInfo
{
    public required ScoreSaberId Id { get; init; }
    public required string Name { get; init; }
    public required string ProfilePicture { get; init; }
    public required string Country { get; init; }
    public required int Permissions { get; init; }
    public required string Role { get; init; }
}