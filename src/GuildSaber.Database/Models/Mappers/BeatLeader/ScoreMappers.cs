using GuildSaber.Common.Services.BeatLeader.Models.Responses;
using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.StrongTypes;
using ScoreStatistics = GuildSaber.Database.Models.Server.Scores.ScoreStatistics;

namespace GuildSaber.Database.Models.Mappers.BeatLeader;

public static class ScoreMappers
{
    public static BeatLeaderScore Map<T>(
        this T uploadScore, Player.PlayerId playerId,
        SongDifficulty.SongDifficultyId songDifficultyId) where T : IUnprocessedScore => new()
    {
        PlayerId = playerId,
        SongDifficultyId = songDifficultyId,
        /* BeatLeaderScoreId can't exist if the score is not processed by beatleader */
        BeatLeaderScoreId = null,

        BaseScore = BaseScore.CreateUnsafe(uploadScore.BaseScore).Value,
        Modifiers = ModifiersMapper.ToModifiers(uploadScore.Modifiers),
        SetAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(uploadScore.TimeSet)),

        MaxCombo = null,
        IsFullCombo = uploadScore.FullCombo,
        BadCuts = uploadScore.BadCuts,
        MissedNotes = uploadScore.MissedNotes,
        Statistics = null,
        HMD = uploadScore.Hmd.Map()
    };

    public static BeatLeaderScore Map<T>(
        this T score, Player.PlayerId playerId,
        SongDifficulty.SongDifficultyId songDifficultyId,
        ScoreStatistics? scoreStatistics) where T : IProcessedScore => new()
    {
        PlayerId = playerId,
        SongDifficultyId = songDifficultyId,
        BeatLeaderScoreId = score.Id,

        BaseScore = BaseScore.CreateUnsafe(score.BaseScore).Value,
        Modifiers = ModifiersMapper.ToModifiers(score.Modifiers),
        SetAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(score.TimeSet)),

        MaxCombo = score.MaxCombo,
        IsFullCombo = score.FullCombo,
        BadCuts = score.BadCuts,
        MissedNotes = score.MissedNotes,
        Statistics = scoreStatistics,
        HMD = score.Hmd.Map()
    };
}