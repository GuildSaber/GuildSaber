using GuildSaber.Common.Services.BeatLeader.Models.Responses;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.StrongTypes;
using ScoreStatistics = GuildSaber.Database.Models.Server.Scores.ScoreStatistics;

namespace GuildSaber.Database.Models.Mappers.BeatLeader;

public static class ScoreMappers
{
    public static async ValueTask<BeatLeaderScore> MapAsync<T>(
        this T uploadScore, PlayerId playerId,
        SongDifficulty.SongDifficultyId songDifficultyId,
        Func<BeatLeaderHmdId, CancellationToken, ValueTask<HmdId?>> resolveHmdId,
        Func<BeatLeaderControllerId, CancellationToken, ValueTask<ControllerId?>> resolveControllerId,
        CancellationToken token) where T : IUnprocessedScore
    {
        var controllerId = await resolveControllerId(uploadScore.Controller, token);
        return new BeatLeaderScore
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
            HmdId = await resolveHmdId(uploadScore.Hmd, token),
            LeftControllerId = controllerId,
            RightControllerId = controllerId
        };
    }

    public static async ValueTask<BeatLeaderScore> MapAsync<T>(
        this T score, PlayerId playerId,
        SongDifficulty.SongDifficultyId songDifficultyId,
        ScoreStatistics? scoreStatistics,
        Func<BeatLeaderHmdId, CancellationToken, ValueTask<HmdId?>> resolveHmdId,
        Func<BeatLeaderControllerId, CancellationToken, ValueTask<ControllerId?>> resolveControllerId,
        CancellationToken token) where T : IProcessedScore
    {
        var controllerId = await resolveControllerId(score.Controller, token);
        return new BeatLeaderScore
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
            HmdId = await resolveHmdId(score.Hmd, token),
            LeftControllerId = controllerId,
            RightControllerId = controllerId
        };
    }
}