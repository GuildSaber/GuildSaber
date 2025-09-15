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
        this UploadScoreResponse uploadScore, Player.PlayerId playerId,
        SongDifficulty.SongDifficultyId songDifficultyId) => new()
    {
        PlayerId = playerId,
        SongDifficultyId = songDifficultyId,
        /* BeatLeaderScoreId can't exist if the score is not processed by beatleader */
        BeatLeaderScoreId = null,

        BaseScore = BaseScore.CreateUnsafe((ulong)uploadScore.BaseScore).Value,
        Modifiers = ToModifiers(uploadScore.Modifiers),
        SetAt = DateTimeOffset.FromUnixTimeMilliseconds(uint.Parse(uploadScore.TimeSet)),

        MaxCombo = null,
        IsFullCombo = uploadScore.FullCombo,
        BadCuts = (uint)uploadScore.BadCuts,
        MissedNotes = (uint)uploadScore.MissedNotes,
        ScoreStatistics = null,
        HMD = uploadScore.Hmd.Map()
    };

    public static BeatLeaderScore Map(
        this AcceptedScoreResponse acceptedScore, ScoreStatistics? scoreStatistics, Player.PlayerId playerId,
        SongDifficulty.SongDifficultyId songDifficultyId) => new()
    {
        PlayerId = playerId,
        SongDifficultyId = songDifficultyId,
        BeatLeaderScoreId = acceptedScore.Id,

        BaseScore = BaseScore.CreateUnsafe((ulong)acceptedScore.BaseScore).Value,
        Modifiers = ToModifiers(acceptedScore.Modifiers),
        SetAt = DateTimeOffset.FromUnixTimeMilliseconds(uint.Parse(acceptedScore.TimeSet)),

        MaxCombo = (uint?)acceptedScore.MaxCombo,
        IsFullCombo = acceptedScore.FullCombo,
        BadCuts = (uint)acceptedScore.BadCuts,
        MissedNotes = (uint)acceptedScore.MissedNotes,
        ScoreStatistics = scoreStatistics,
        HMD = acceptedScore.Hmd.Map()
    };

    public static BeatLeaderScore Map(
        this RejectedScoreResponse rejectedScore, ScoreStatistics? scoreStatistics, Player.PlayerId playerId,
        SongDifficulty.SongDifficultyId songDifficultyId) => new()
    {
        PlayerId = playerId,
        SongDifficultyId = songDifficultyId,
        BeatLeaderScoreId = rejectedScore.Id,

        BaseScore = BaseScore.CreateUnsafe((ulong)rejectedScore.BaseScore).Value,
        Modifiers = ToModifiers(rejectedScore.Modifiers),
        SetAt = DateTimeOffset.FromUnixTimeMilliseconds(uint.Parse(rejectedScore.TimeSet)),

        MaxCombo = (uint?)rejectedScore.MaxCombo,
        IsFullCombo = rejectedScore.FullCombo,
        BadCuts = (uint)rejectedScore.BadCuts,
        MissedNotes = (uint)rejectedScore.MissedNotes,
        ScoreStatistics = scoreStatistics,
        HMD = rejectedScore.Hmd.Map()
    };
}