using GuildSaber.Common.Helpers;
using GuildSaber.Common.Services.BeatLeader.Models.Responses;
using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.StrongTypes;
using ScoreStatistics = GuildSaber.Database.Models.Server.Scores.ScoreStatistics;

namespace GuildSaber.Database.Models.Mappers.BeatLeader;

public static class ScoreMappers
{
    private static readonly char[] _separator = { ',' };

    public static AbstractScore.EModifiers ToModifiers(string input) => input
        .Split(_separator, StringSplitOptions.RemoveEmptyEntries)
        .Aggregate<string, AbstractScore.EModifiers>(0, (current, modifier) => current | modifier switch
        {
            "NO" => AbstractScore.EModifiers.NoObstacles,
            "NB" => AbstractScore.EModifiers.NoBombs,
            "NF" => AbstractScore.EModifiers.NoFail,
            "SS" => AbstractScore.EModifiers.SlowerSong,
            "BE" => AbstractScore.EModifiers.BatteryEnergy,
            "IF" => AbstractScore.EModifiers.InstaFail,
            "SC" => AbstractScore.EModifiers.SmallNotes,
            "PM" => AbstractScore.EModifiers.ProMode,
            "FS" => AbstractScore.EModifiers.FasterSong,
            "SA" => AbstractScore.EModifiers.StrictAngles,
            "DA" => AbstractScore.EModifiers.DisappearingArrows,
            "GN" => AbstractScore.EModifiers.GhostNotes,
            "NA" => AbstractScore.EModifiers.NoArrows,
            "SF" => AbstractScore.EModifiers.SuperFastSong,
            "OD" => AbstractScore.EModifiers.OldDots,
            "OP" => AbstractScore.EModifiers.OffPlatform,
            _ => AbstractScore.EModifiers.Unk
        });

    public static string ToModifiersString(AbstractScore.EModifiers modifiers) => modifiers
        .GetFlags()
        .Aggregate<AbstractScore.EModifiers, string>("", (current, modifier) => current + modifier switch
        {
            AbstractScore.EModifiers.NoObstacles => "NO,",
            AbstractScore.EModifiers.NoBombs => "NB,",
            AbstractScore.EModifiers.NoFail => "NF,",
            AbstractScore.EModifiers.SlowerSong => "SS,",
            AbstractScore.EModifiers.BatteryEnergy => "BE,",
            AbstractScore.EModifiers.InstaFail => "IF,",
            AbstractScore.EModifiers.SmallNotes => "SC,",
            AbstractScore.EModifiers.ProMode => "PM,",
            AbstractScore.EModifiers.FasterSong => "FS,",
            AbstractScore.EModifiers.StrictAngles => "SA,",
            AbstractScore.EModifiers.DisappearingArrows => "DA,",
            AbstractScore.EModifiers.GhostNotes => "GN,",
            AbstractScore.EModifiers.NoArrows => "NA,",
            AbstractScore.EModifiers.SuperFastSong => "SF,",
            AbstractScore.EModifiers.OldDots => "OD,",
            AbstractScore.EModifiers.OffPlatform => "OP,",
            _ => ""
        });

    public static BeatLeaderScore Map(
        this UploadedScore uploadScore, Player.PlayerId playerId,
        SongDifficulty.SongDifficultyId songDifficultyId) => new()
    {
        PlayerId = playerId,
        SongDifficultyId = songDifficultyId,
        /* BeatLeaderScoreId can't exist if the score is not processed by beatleader */
        BeatLeaderScoreId = null,

        BaseScore = BaseScore.CreateUnsafe(uploadScore.BaseScore).Value,
        Modifiers = ToModifiers(uploadScore.Modifiers),
        SetAt = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(uploadScore.TimeSet)),

        MaxCombo = null,
        IsFullCombo = uploadScore.FullCombo,
        BadCuts = uploadScore.BadCuts,
        MissedNotes = uploadScore.MissedNotes,
        ScoreStatistics = null,
        HMD = uploadScore.Hmd.Map()
    };

    public static BeatLeaderScore Map(
        this AcceptedScore acceptedScore, ScoreStatistics? scoreStatistics, Player.PlayerId playerId,
        SongDifficulty.SongDifficultyId songDifficultyId) => new()
    {
        PlayerId = playerId,
        SongDifficultyId = songDifficultyId,
        BeatLeaderScoreId = acceptedScore.Id,

        BaseScore = BaseScore.CreateUnsafe(acceptedScore.BaseScore).Value,
        Modifiers = ToModifiers(acceptedScore.Modifiers),
        SetAt = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(acceptedScore.TimeSet)),

        MaxCombo = acceptedScore.MaxCombo,
        IsFullCombo = acceptedScore.FullCombo,
        BadCuts = acceptedScore.BadCuts,
        MissedNotes = acceptedScore.MissedNotes,
        ScoreStatistics = scoreStatistics,
        HMD = acceptedScore.Hmd.Map()
    };

    public static BeatLeaderScore Map(
        this RejectedScore rejectedScore, ScoreStatistics? scoreStatistics, Player.PlayerId playerId,
        SongDifficulty.SongDifficultyId songDifficultyId) => new()
    {
        PlayerId = playerId,
        SongDifficultyId = songDifficultyId,
        BeatLeaderScoreId = rejectedScore.Id,

        BaseScore = BaseScore.CreateUnsafe(rejectedScore.BaseScore).Value,
        Modifiers = ToModifiers(rejectedScore.Modifiers),
        SetAt = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(rejectedScore.TimeSet)),

        MaxCombo = rejectedScore.MaxCombo,
        IsFullCombo = rejectedScore.FullCombo,
        BadCuts = rejectedScore.BadCuts,
        MissedNotes = rejectedScore.MissedNotes,
        ScoreStatistics = scoreStatistics,
        HMD = rejectedScore.Hmd.Map()
    };
}